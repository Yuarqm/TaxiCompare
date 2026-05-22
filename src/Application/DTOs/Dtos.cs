namespace TaxiCompare.Application.DTOs;

public record PriceComparisonRequest(
    string OriginAddress,
    double OriginLat,
    double OriginLng,
    string DestinationAddress,
    double DestinationLat,
    double DestinationLng,
    string? PreferredClass = null,
    double? DistanceKm = null,  // real road distance from routing API
    string? OriginCity = null   // город для запроса погоды (необязательно)
);

public record PriceComparisonResult(
    Guid RideRequestId,
    IEnumerable<ProviderPriceDto> Prices,
    ProviderPriceDto? BestDeal,
    DateTime RetrievedAt
);

public record ProviderPriceDto(
    Guid ProviderId,
    string ProviderName,
    string ProviderSlug,
    string LogoUrl,
    decimal Price,
    string Currency,
    int EtaMinutes,
    string VehicleClass,
    double SurgeMultiplier,
    double ProviderRating,
    bool IsAvailable,
    bool IsBestDeal
);

public record PriceHistoryDto(
    Guid ProviderId,
    string ProviderName,
    IEnumerable<PriceDataPoint> DataPoints
);

public record PriceDataPoint(DateTime Timestamp, decimal Price, double Surge);

public record RouteDto(
    string OriginAddress,
    string DestinationAddress,
    double OriginLat,
    double OriginLng,
    double DestinationLat,
    double DestinationLng,
    int EstimatedDurationMinutes,
    double DistanceKm
);

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    DateTime CreatedAt
);

public record AuthResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);

public record RegisterRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber
);

public record LoginRequest(string Email, string Password);

public record NotificationDto(
    Guid Id,
    string Title,
    string Message,
    string Type,
    bool IsRead,
    DateTime CreatedAt
);

public record PopularRouteDto(
    string OriginAddress,
    string DestinationAddress,
    int RequestCount,
    decimal AveragePrice
);

public record AnalyticsSummaryDto(
    int TotalSearches,
    decimal AveragePrice,
    string MostPopularProvider,
    IEnumerable<PopularRouteDto> TopRoutes
);

public record WeatherInfoDto(
    string City,
    string Condition,
    string ConditionRu,
    double TemperatureCelsius,
    double WindSpeedKmh,
    decimal Multiplier
)
{
    public bool HasSurcharge => Multiplier > 1.0m;
    public int SurchargePercent => HasSurcharge ? (int)Math.Round((Multiplier - 1.0m) * 100) : 0;
}

// Расширенный результат сравнения с погодными данными
public record PriceComparisonResultWithWeather(
    Guid RideRequestId,
    IEnumerable<ProviderPriceDtoWithWeather> Prices,
    ProviderPriceDtoWithWeather? BestDeal,
    DateTime RetrievedAt,
    WeatherInfoDto Weather
);

public record ProviderPriceDtoWithWeather(
    Guid ProviderId,
    string ProviderName,
    string ProviderSlug,
    string LogoUrl,
    decimal Price,
    decimal BasePrice,
    string Currency,
    int EtaMinutes,
    string VehicleClass,
    double SurgeMultiplier,
    double ProviderRating,
    bool IsAvailable,
    bool IsBestDeal,
    decimal? WeatherBoost,
    int WeatherSurchargePercent,
    decimal WeatherSurcharge
);
