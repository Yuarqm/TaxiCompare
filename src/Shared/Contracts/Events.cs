namespace TaxiCompare.Contracts.Events;

/// <summary>Published when new prices are fetched for a route</summary>
public record PricesFetchedEvent(
    Guid RideRequestId,
    Guid UserId,
    string OriginAddress,
    string DestinationAddress,
    IEnumerable<ProviderPriceContract> Prices,
    DateTime OccurredAt
);

/// <summary>Published when a user's price alert threshold is hit</summary>
public record PriceAlertTriggeredEvent(
    Guid UserId,
    string ProviderName,
    decimal CurrentPrice,
    decimal ThresholdPrice,
    string OriginAddress,
    string DestinationAddress,
    DateTime OccurredAt
);

/// <summary>Published when a user registers</summary>
public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string FirstName,
    DateTime OccurredAt
);

public record ProviderPriceContract(
    string ProviderName,
    string ProviderSlug,
    decimal Price,
    string Currency,
    int EtaMinutes,
    double SurgeMultiplier,
    bool IsAvailable
);
