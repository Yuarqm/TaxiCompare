using MediatR;
using Microsoft.Extensions.Logging;
using TaxiCompare.Pricing.Application.Interfaces;
using TaxiCompare.Pricing.Domain.Entities;
using TaxiCompare.Pricing.Domain.Interfaces;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Application.Commands;

public class GetPriceComparisonHandler : IRequestHandler<GetPriceComparisonCommand, PriceComparisonResultDto>
{
    private readonly IPriceAggregationService _aggregationService;
    private readonly IRideRequestRepository _rideRequestRepo;
    private readonly IPriceSnapshotRepository _snapshotRepo;
    private readonly IProviderRepository _providerRepo;
    private readonly ICacheService _cache;
    private readonly ILogger<GetPriceComparisonHandler> _logger;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    public GetPriceComparisonHandler(
        IPriceAggregationService aggregationService,
        IRideRequestRepository rideRequestRepo,
        IPriceSnapshotRepository snapshotRepo,
        IProviderRepository providerRepo,
        ICacheService cache,
        ILogger<GetPriceComparisonHandler> logger)
    {
        _aggregationService = aggregationService;
        _rideRequestRepo = rideRequestRepo;
        _snapshotRepo = snapshotRepo;
        _providerRepo = providerRepo;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PriceComparisonResultDto> Handle(
        GetPriceComparisonCommand cmd, CancellationToken ct)
    {
        var cacheKey = $"price:{cmd.OriginLat:F4}:{cmd.OriginLng:F4}:{cmd.DestinationLat:F4}:{cmd.DestinationLng:F4}:{cmd.PreferredClass}";

        var cached = await _cache.GetAsync<PriceComparisonResultDto>(cacheKey, ct);
        if (cached is not null)
        {
            _logger.LogInformation("Cache hit for price comparison {CacheKey}", cacheKey);
            return cached;
        }

        var request = RideRequest.Create(
            cmd.UserId, cmd.Origin, cmd.OriginLat, cmd.OriginLng,
            cmd.Destination, cmd.DestinationLat, cmd.DestinationLng,
            cmd.PreferredClass);

        await _rideRequestRepo.AddAsync(request, ct);
        await _rideRequestRepo.SaveChangesAsync(ct);

        var rideRequestDto = new RideRequestDto(
            cmd.Origin, cmd.OriginLat, cmd.OriginLng,
            cmd.Destination, cmd.DestinationLat, cmd.DestinationLng,
            cmd.PreferredClass);

        var result = await _aggregationService.AggregateAsync(rideRequestDto, ct);

        // Persist snapshots
        var providers = await _providerRepo.GetActiveProvidersAsync(ct);
        var snapshots = result.Quotes.Select(q =>
        {
            var provider = providers.FirstOrDefault(p => p.Slug == q.ProviderId);
            if (provider is null) return null;
            return PriceSnapshot.Create(request.Id, provider.Id,
                q.MinPrice, q.MaxPrice, q.Currency,
                q.EtaMinutes, q.VehicleClass, q.SurgeMultiplier, q.DeepLinkUrl);
        }).OfType<PriceSnapshot>();

        await _snapshotRepo.AddRangeAsync(snapshots, ct);
        await _snapshotRepo.SaveChangesAsync(ct);

        await _cache.SetAsync(cacheKey, result, CacheTtl, ct);

        _logger.LogInformation("Price comparison completed for {Origin} -> {Destination}, {Count} providers",
            cmd.Origin, cmd.Destination, result.Quotes.Count);

        return result;
    }
}
