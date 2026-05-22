using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Application.Interfaces;

/// <summary>
/// Core abstraction for all taxi provider integrations.
/// Each provider (Uber, Bolt, Yandex, etc.) implements this interface.
/// </summary>
public interface ITaxiProvider
{
    string ProviderId { get; }
    string ProviderName { get; }
    string LogoUrl { get; }

    /// <summary>
    /// Fetch price quotes for a ride. Returns null if provider unavailable.
    /// </summary>
    Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(RideRequestDto request, CancellationToken ct = default);

    /// <summary>
    /// Check if provider supports the given coordinates region.
    /// </summary>
    Task<bool> IsAvailableInRegionAsync(double lat, double lng, CancellationToken ct = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
}

public interface IPriceAggregationService
{
    Task<PriceComparisonResultDto> AggregateAsync(RideRequestDto request, CancellationToken ct = default);
}

public interface IRouteService
{
    Task<RouteInfoDto> GetRouteInfoAsync(double originLat, double originLng,
        double destLat, double destLng, CancellationToken ct = default);
}

public interface IAiPricePredictionService
{
    Task<PricePredictionDto> PredictOptimalOrderTimeAsync(
        string origin, string destination, CancellationToken ct = default);
}

public record PricePredictionDto(
    string Recommendation,
    DateTime SuggestedOrderTime,
    decimal ExpectedSavings,
    string Reasoning,
    IReadOnlyList<HourlyForecastDto> HourlyForecast
);

public record HourlyForecastDto(
    DateTime Hour,
    decimal PredictedPrice,
    double ConfidenceScore,
    string Trend  // "rising" | "falling" | "stable"
);
