using MediatR;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Application.Queries;

public record GetPriceHistoryQuery(
    string Origin,
    string Destination,
    string? ProviderId,
    string TimeRange  // "1h" | "24h" | "7d"
) : IRequest<IReadOnlyList<PriceHistoryPointDto>>;

public record GetPopularRoutesQuery(
    int Limit = 10
) : IRequest<IReadOnlyList<PopularRouteDto>>;

public record GetAiPricePredictionQuery(
    string Origin,
    string Destination
) : IRequest<PricePredictionDto>;

public record PopularRouteDto(
    string Origin,
    string Destination,
    int TripCount,
    decimal AveragePrice,
    string Currency
);
