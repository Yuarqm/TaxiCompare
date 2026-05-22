using TaxiCompare.Application.DTOs;

namespace TaxiCompare.Application.Interfaces;

/// <summary>Core abstraction for all taxi provider integrations</summary>
public interface ITaxiProvider
{
    string ProviderName { get; }
    string ProviderSlug { get; }
    bool IsAvailableInRegion(double lat, double lng);
    Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken cancellationToken = default);
}

public interface IPricingAggregator
{
    Task<PriceComparisonResult> GetAllPricesAsync(PriceComparisonRequest request, CancellationToken cancellationToken = default);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
}

public interface ITokenService
{
    string GenerateAccessToken(UserDto user);
    string GenerateRefreshToken();
    UserDto? ValidateToken(string token);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface INotificationSender
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default);
    Task SendPushAsync(Guid userId, string title, string message, CancellationToken cancellationToken = default);
}

public interface IPriceAlertService
{
    Task CheckAndTriggerAlertsAsync(Guid rideRequestId, IEnumerable<ProviderPriceDto> prices, CancellationToken cancellationToken = default);
}

public interface IGeocodingService
{
    Task<(double Lat, double Lng)?> GeocodeAsync(string address, CancellationToken cancellationToken = default);
    Task<string?> ReverseGeocodeAsync(double lat, double lng, CancellationToken cancellationToken = default);
}

public interface IWeatherService
{
    Task<TaxiCompare.Domain.Entities.WeatherCondition> GetCurrentWeatherAsync(string city, CancellationToken ct = default);
    Task<decimal> GetWeatherMultiplierAsync(string city, CancellationToken ct = default);
}
