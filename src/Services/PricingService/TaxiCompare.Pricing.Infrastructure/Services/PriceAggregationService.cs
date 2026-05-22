using Microsoft.Extensions.Logging;
using TaxiCompare.Pricing.Application.Interfaces;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Infrastructure.Services;

public class PriceAggregationService : IPriceAggregationService
{
    private readonly IEnumerable<ITaxiProvider> _providers;
    private readonly ILogger<PriceAggregationService> _logger;

    public PriceAggregationService(
        IEnumerable<ITaxiProvider> providers,
        ILogger<PriceAggregationService> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public async Task<PriceComparisonResultDto> AggregateAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        _logger.LogInformation("Aggregating prices from {Count} providers for {Origin} -> {Destination}",
            _providers.Count(), request.Origin, request.Destination);

        var requestId = Guid.NewGuid().ToString();
        var timeout = TimeSpan.FromSeconds(10);

        // Fetch from all providers in parallel with timeout
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);

        var tasks = _providers.Select(async provider =>
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var quotes = await provider.GetQuotesAsync(request, cts.Token);
                sw.Stop();
                _logger.LogInformation("Provider {Provider} returned {Count} quotes in {Ms}ms",
                    provider.ProviderId, quotes.Count, sw.ElapsedMilliseconds);
                return quotes;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Provider {Provider} failed", provider.ProviderId);
                return Array.Empty<PriceQuoteDto>();
            }
        });

        var results = await Task.WhenAll(tasks);
        var allQuotes = results.SelectMany(q => q).ToList();

        if (!allQuotes.Any())
        {
            return new PriceComparisonResultDto(requestId, request, Array.Empty<PriceQuoteDto>(),
                "", "", DateTime.UtcNow);
        }

        var bestValue = allQuotes.MinBy(q => q.MinPrice)?.ProviderId ?? "";
        var fastest = allQuotes.MinBy(q => q.EtaMinutes)?.ProviderId ?? "";

        return new PriceComparisonResultDto(
            requestId, request,
            allQuotes.OrderBy(q => q.MinPrice).ToList(),
            bestValue, fastest,
            DateTime.UtcNow
        );
    }
}
