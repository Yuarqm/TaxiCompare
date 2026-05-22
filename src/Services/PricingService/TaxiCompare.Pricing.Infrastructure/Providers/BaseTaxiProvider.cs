using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using TaxiCompare.Pricing.Application.Interfaces;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Infrastructure.Providers;

/// <summary>
/// Base class for all taxi providers with Polly retry + circuit breaker built in.
/// </summary>
public abstract class BaseTaxiProvider : ITaxiProvider
{
    protected readonly HttpClient HttpClient;
    protected readonly ILogger Logger;

    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public abstract string ProviderId { get; }
    public abstract string ProviderName { get; }
    public abstract string LogoUrl { get; }

    protected BaseTaxiProvider(HttpClient httpClient, ILogger logger)
    {
        HttpClient = httpClient;
        Logger = logger;

        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(3,
                attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                (ex, delay, attempt, _) =>
                    Logger.LogWarning(ex, "Retry {Attempt} for {Provider} after {Delay}ms",
                        attempt, ProviderId, delay.TotalMilliseconds));

        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(5, TimeSpan.FromMinutes(1),
                (ex, duration) =>
                    Logger.LogError(ex, "Circuit breaker OPEN for {Provider} for {Duration}s",
                        ProviderId, duration.TotalSeconds),
                () => Logger.LogInformation("Circuit breaker CLOSED for {Provider}", ProviderId));
    }

    public abstract Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default);

    public virtual Task<bool> IsAvailableInRegionAsync(double lat, double lng, CancellationToken ct = default)
        => Task.FromResult(true);

    protected async Task<T?> ExecuteWithResilienceAsync<T>(
        Func<CancellationToken, Task<T>> action, CancellationToken ct)
    {
        try
        {
            return await _retryPolicy.WrapAsync(_circuitBreaker)
                .ExecuteAsync(action, ct);
        }
        catch (BrokenCircuitException)
        {
            Logger.LogWarning("Circuit breaker is OPEN for {Provider}, skipping", ProviderId);
            return default;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Provider {Provider} failed after retries", ProviderId);
            return default;
        }
    }
}
