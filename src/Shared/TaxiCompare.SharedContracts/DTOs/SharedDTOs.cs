namespace TaxiCompare.SharedContracts.DTOs;

public record RideRequestDto(
    string Origin,
    double OriginLat,
    double OriginLng,
    string Destination,
    double DestinationLat,
    double DestinationLng,
    string? PreferredClass = null
);

public record PriceQuoteDto(
    string ProviderId,
    string ProviderName,
    string ProviderLogoUrl,
    decimal MinPrice,
    decimal MaxPrice,
    string Currency,
    int EtaMinutes,
    string VehicleClass,
    double SurgeMultiplier,
    double ProviderRating,
    string DeepLinkUrl,
    DateTime FetchedAt
);

public record PriceComparisonResultDto(
    string RequestId,
    RideRequestDto Request,
    IReadOnlyList<PriceQuoteDto> Quotes,
    string BestValueProviderId,
    string FastestProviderId,
    DateTime GeneratedAt
);

public record RouteInfoDto(
    double DistanceKm,
    int DurationMinutes,
    string EncodedPolyline
);

public record UserDto(
    Guid Id,
    string Email,
    string Name,
    string? AvatarUrl
);

public record PriceHistoryPointDto(
    string ProviderId,
    decimal Price,
    string Currency,
    double SurgeMultiplier,
    DateTime Timestamp
);
