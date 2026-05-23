using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;

namespace TaxiCompare.Infrastructure.Services;

/// <summary>
/// Заглушка — уведомления о ценах не реализованы, но сервис должен быть зарегистрирован в DI.
/// </summary>
public class NoOpPriceAlertService : IPriceAlertService
{
    public Task CheckAndTriggerAlertsAsync(
        Guid rideRequestId,
        IEnumerable<ProviderPriceDto> prices,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
