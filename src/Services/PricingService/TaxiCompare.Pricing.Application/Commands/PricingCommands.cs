using MediatR;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Application.Commands;

// ─── GetPriceComparison ───────────────────────────────────────────────────────

public record GetPriceComparisonCommand(
    string UserId,
    string Origin,
    double OriginLat,
    double OriginLng,
    string Destination,
    double DestinationLat,
    double DestinationLng,
    string? PreferredClass = null
) : IRequest<PriceComparisonResultDto>;

// ─── SavePriceAlert ───────────────────────────────────────────────────────────

public record CreatePriceAlertCommand(
    string UserId,
    string Origin,
    string Destination,
    string ProviderId,
    decimal TargetPrice,
    string Currency
) : IRequest<Guid>;

// ─── UpdateUserPreferences ────────────────────────────────────────────────────

public record UpdateUserPreferencesCommand(
    string UserId,
    string Currency,
    bool EmailNotifications,
    bool PushNotifications,
    decimal? PriceAlertThreshold
) : IRequest<Unit>;
