using MediatR;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Application.Queries;
using TaxiCompare.Domain.Interfaces;

namespace TaxiCompare.Application.Handlers;

public class GetPricesQueryHandler : IRequestHandler<GetPricesQuery, PriceComparisonResult>
{
    private readonly IPricingAggregator _aggregator;
    private readonly ICacheService _cache;

    public GetPricesQueryHandler(IPricingAggregator aggregator, ICacheService cache)
    {
        _aggregator = aggregator;
        _cache = cache;
    }

    public async Task<PriceComparisonResult> Handle(GetPricesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"prices:{request.Request.OriginLat:F4}:{request.Request.OriginLng:F4}:{request.Request.DestinationLat:F4}:{request.Request.DestinationLng:F4}";
            var cached = await _cache.GetAsync<PriceComparisonResult>(cacheKey, cancellationToken);
            if (cached is not null) return cached;

            var result = await _aggregator.GetAllPricesAsync(request.Request, cancellationToken);
            await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2), cancellationToken);
            return result;
        }
        catch (Exception)
        {
            return await _aggregator.GetAllPricesAsync(request.Request, cancellationToken);
        }
    }
}

public class GetPriceHistoryQueryHandler : IRequestHandler<GetPriceHistoryQuery, IEnumerable<PriceHistoryDto>>
{
    private readonly IPriceSnapshotRepository _snapshots;
    private readonly IProviderRepository _providers;

    public GetPriceHistoryQueryHandler(IPriceSnapshotRepository snapshots, IProviderRepository providers)
    {
        _snapshots = snapshots;
        _providers = providers;
    }

    public async Task<IEnumerable<PriceHistoryDto>> Handle(GetPriceHistoryQuery request, CancellationToken cancellationToken)
    {
        var from = request.Period switch
        {
            "1h"  => DateTime.UtcNow.AddHours(-1),
            "7d"  => DateTime.UtcNow.AddDays(-7),
            _     => DateTime.UtcNow.AddDays(-1)
        };

        var snapshots = await _snapshots.GetHistoryAsync(request.ProviderId, from, DateTime.UtcNow, cancellationToken);
        var provider  = await _providers.GetByIdAsync(request.ProviderId, cancellationToken);

        return new[]
        {
            new PriceHistoryDto(
                request.ProviderId,
                provider?.Name ?? "Unknown",
                snapshots.Select(s => new PriceDataPoint(s.RecordedAt, s.Price, s.SurgeMultiplier)))
        };
    }
}

public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IUserRepository _users;
    public GetUserByIdQueryHandler(IUserRepository users) => _users = users;

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return null;
        return new UserDto(user.Id, user.Email, user.FirstName, user.LastName, user.PhoneNumber, user.CreatedAt);
    }
}

public class GetUserRideHistoryQueryHandler : IRequestHandler<GetUserRideHistoryQuery, IEnumerable<RideRequestSummaryDto>>
{
    private readonly IRideRequestRepository _rides;
    public GetUserRideHistoryQueryHandler(IRideRequestRepository rides) => _rides = rides;

    public async Task<IEnumerable<RideRequestSummaryDto>> Handle(GetUserRideHistoryQuery request, CancellationToken cancellationToken)
    {
        var rides = await _rides.GetByUserIdAsync(request.UserId, cancellationToken);
        return rides.Select(r => new RideRequestSummaryDto(
            r.Id, r.OriginAddress, r.DestinationAddress, r.RequestedAt,
            r.PriceSnapshots.Count,
            r.PriceSnapshots.Any() ? r.PriceSnapshots.Min(p => p.Price) : null,
            r.Status.ToString(),
            r.OrderedProviderName, r.OrderedProviderSlug,
            r.OrderedVehicleClass, r.OrderedPrice, r.OrderedAt));
    }
}

public class GetAnalyticsSummaryQueryHandler : IRequestHandler<GetAnalyticsSummaryQuery, AnalyticsSummaryDto>
{
    private readonly IRideRequestRepository _rides;
    public GetAnalyticsSummaryQueryHandler(IRideRequestRepository rides) => _rides = rides;

    public async Task<AnalyticsSummaryDto> Handle(GetAnalyticsSummaryQuery request, CancellationToken cancellationToken)
    {
        var rides  = await _rides.GetAllAsync(cancellationToken);
        var routes = await _rides.GetPopularRoutesAsync(5, cancellationToken);
        return new AnalyticsSummaryDto(
            rides.Count(), 0m, "Uber",
            routes.Select(r => new PopularRouteDto(r.OriginAddress, r.DestinationAddress, 0, 0)));
    }
}

public class GetPopularRoutesQueryHandler : IRequestHandler<GetPopularRoutesQuery, IEnumerable<PopularRouteDto>>
{
    private readonly IRideRequestRepository _rides;
    public GetPopularRoutesQueryHandler(IRideRequestRepository rides) => _rides = rides;

    public async Task<IEnumerable<PopularRouteDto>> Handle(GetPopularRoutesQuery request, CancellationToken cancellationToken)
    {
        var routes = await _rides.GetPopularRoutesAsync(request.Count, cancellationToken);
        return routes.Select(r => new PopularRouteDto(r.OriginAddress, r.DestinationAddress, 0, 0));
    }
}

public class GetUserNotificationsQueryHandler : IRequestHandler<GetUserNotificationsQuery, IEnumerable<NotificationDto>>
{
    private readonly IUserRepository _users;
    public GetUserNotificationsQueryHandler(IUserRepository users) => _users = users;

    public async Task<IEnumerable<NotificationDto>> Handle(GetUserNotificationsQuery request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return Enumerable.Empty<NotificationDto>();
        return user.Notifications.Select(n => new NotificationDto(
            n.Id, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.CreatedAt));
    }
}

public class MarkNotificationReadCommandHandler : IRequestHandler<TaxiCompare.Application.Commands.MarkNotificationReadCommand, bool>
{
    private readonly IUserRepository _users;
    public MarkNotificationReadCommandHandler(IUserRepository users) => _users = users;

    public async Task<bool> Handle(TaxiCompare.Application.Commands.MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null) return false;
        var notif = user.Notifications.FirstOrDefault(n => n.Id == request.NotificationId);
        if (notif is null) return false;
        notif.MarkRead();
        await _users.SaveChangesAsync(cancellationToken);
        return true;
    }
}
