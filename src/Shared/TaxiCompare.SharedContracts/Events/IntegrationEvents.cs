namespace TaxiCompare.SharedContracts.Events;

public abstract record IntegrationEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record PricesUpdatedEvent(
    string RequestId,
    string UserId,
    string ProviderId,
    decimal NewPrice,
    decimal OldPrice,
    string Currency
) : IntegrationEvent;

public record PriceDropAlertEvent(
    string UserId,
    string ProviderId,
    decimal DropAmount,
    decimal CurrentPrice,
    string Currency,
    string Origin,
    string Destination
) : IntegrationEvent;

public record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string Name
) : IntegrationEvent;

public record RideRequestCreatedEvent(
    string RequestId,
    string UserId,
    string Origin,
    string Destination,
    DateTime RequestedAt
) : IntegrationEvent;
