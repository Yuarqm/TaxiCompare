using MediatR;
using TaxiCompare.Application.Commands;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Domain.Entities;
using TaxiCompare.Domain.Interfaces;

namespace TaxiCompare.Application.Handlers;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public RegisterCommandHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var exists = await _users.ExistsByEmailAsync(request.Request.Email, cancellationToken);
        if (exists) throw new InvalidOperationException("Email already registered");

        var hash = _hasher.Hash(request.Request.Password);
        var user = User.Create(request.Request.Email, hash, request.Request.FirstName,
            request.Request.LastName, request.Request.PhoneNumber);

        await _users.AddAsync(user, cancellationToken);
        await _users.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.CreatedAt);
        var accessToken = _tokens.GenerateAccessToken(dto);
        var refreshToken = _tokens.GenerateRefreshToken();

        return new AuthResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), dto);
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResult>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;

    public LoginCommandHandler(IUserRepository users, IPasswordHasher hasher, ITokenService tokens)
    {
        _users = users;
        _hasher = hasher;
        _tokens = tokens;
    }

    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(request.Request.Email, cancellationToken);
        if (user is null || !_hasher.Verify(request.Request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials");

        user.UpdateLastLogin();
        await _users.SaveChangesAsync(cancellationToken);

        var dto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.CreatedAt);
        var accessToken = _tokens.GenerateAccessToken(dto);
        var refreshToken = _tokens.GenerateRefreshToken();

        return new AuthResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), dto);
    }
}

public class CreateRideRequestCommandHandler : IRequestHandler<CreateRideRequestCommand, PriceComparisonResult>{
    private readonly IRideRequestRepository _rides;
    private readonly IPriceSnapshotRepository _snapshots;
    private readonly IProviderRepository _providers;
    private readonly IPricingAggregator _aggregator;
    private readonly IPriceAlertService _alertService;

    public CreateRideRequestCommandHandler(IRideRequestRepository rides, IPriceSnapshotRepository snapshots,
        IProviderRepository providers, IPricingAggregator aggregator, IPriceAlertService alertService)
    {
        _rides = rides;
        _snapshots = snapshots;
        _providers = providers;
        _aggregator = aggregator;
        _alertService = alertService;
    }

    public async Task<PriceComparisonResult> Handle(CreateRideRequestCommand request, CancellationToken cancellationToken)
    {
        var rideRequest = RideRequest.Create(
            request.UserId,
            request.Request.OriginAddress, request.Request.OriginLat, request.Request.OriginLng,
            request.Request.DestinationAddress, request.Request.DestinationLat, request.Request.DestinationLng
        );
        await _rides.AddAsync(rideRequest, cancellationToken);
        await _rides.SaveChangesAsync(cancellationToken);

        var result = await _aggregator.GetAllPricesAsync(request.Request, cancellationToken);

        // Persist price snapshots
        var providers = (await _providers.GetActiveAsync(cancellationToken)).ToDictionary(p => p.Slug);
        foreach (var price in result.Prices.Where(p => p.IsAvailable))
        {
            if (!providers.TryGetValue(price.ProviderSlug, out var provider)) continue;
            var snapshot = PriceSnapshot.Create(rideRequest.Id, provider.Id,
                price.Price, price.Currency, price.EtaMinutes, price.VehicleClass, price.SurgeMultiplier);
            await _snapshots.AddAsync(snapshot, cancellationToken);
        }
        await _snapshots.SaveChangesAsync(cancellationToken);

        // Check price alerts
        await _alertService.CheckAndTriggerAlertsAsync(rideRequest.Id, result.Prices, cancellationToken);

        return result with { RideRequestId = rideRequest.Id };
    }
}

public class OrderRideCommandHandler : IRequestHandler<TaxiCompare.Application.Commands.OrderRideCommand, bool>
{
    private readonly IRideRequestRepository _rides;

    public OrderRideCommandHandler(IRideRequestRepository rides) => _rides = rides;

    public async Task<bool> Handle(TaxiCompare.Application.Commands.OrderRideCommand request, CancellationToken cancellationToken)
    {
        var ride = await _rides.GetByIdAsync(request.RideRequestId, cancellationToken);
        // Если заказ ещё не создан (гость/незалогиненный поиск) — создаём заглушку
        if (ride is null)
        {
            // Нет записи — фиксируем факт без RideRequest (не критично)
            return false;
        }

        if (ride.UserId != request.UserId)
            throw new UnauthorizedAccessException("Нет доступа к этой поездке");

        ride.PlaceOrder(request.ProviderName, request.ProviderSlug, request.VehicleClass, request.Price);
        await _rides.SaveChangesAsync(cancellationToken);
        return true;
    }
}
