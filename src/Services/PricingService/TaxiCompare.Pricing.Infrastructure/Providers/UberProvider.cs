using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Infrastructure.Providers;

public class UberProvider : BaseTaxiProvider
{
    public override string ProviderId => "uber";
    public override string ProviderName => "Uber";
    public override string LogoUrl => "https://cdn.taxicompare.app/logos/uber.svg";

    public UberProvider(HttpClient httpClient, ILogger<UberProvider> logger)
        : base(httpClient, logger) { }

    public override async Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        var result = await ExecuteWithResilienceAsync(async token =>
        {
            // In production: call https://api.uber.com/v1.2/estimates/price
            // For now, return realistic mock data
            var response = await HttpClient.GetFromJsonAsync<UberPriceResponse>(
                $"estimates/price?start_latitude={request.OriginLat}&start_longitude={request.OriginLng}" +
                $"&end_latitude={request.DestinationLat}&end_longitude={request.DestinationLng}",
                token);

            if (response?.Prices is null) return Array.Empty<PriceQuoteDto>();

            return response.Prices.Select(p => new PriceQuoteDto(
                ProviderId, ProviderName, LogoUrl,
                p.LowEstimate, p.HighEstimate, p.CurrencyCode,
                p.Duration / 60, MapProductToClass(p.ProductId),
                p.SurgeMultiplier,
                4.5,
                $"uber://action?pickup[latitude]={request.OriginLat}&pickup[longitude]={request.OriginLng}" +
                $"&dropoff[latitude]={request.DestinationLat}&dropoff[longitude]={request.DestinationLng}",
                DateTime.UtcNow
            )).ToArray();
        }, ct);

        return result ?? SimulatePrices(request);
    }

    private static string MapProductToClass(string productId) => productId switch
    {
        var x when x.Contains("uberx", StringComparison.OrdinalIgnoreCase) => "Economy",
        var x when x.Contains("comfort", StringComparison.OrdinalIgnoreCase) => "Comfort",
        var x when x.Contains("black", StringComparison.OrdinalIgnoreCase) => "Business",
        _ => "Economy"
    };

    // Realistic fallback simulation when real API is not configured
    private IReadOnlyList<PriceQuoteDto> SimulatePrices(RideRequestDto request)
    {
        var distance = CalculateDistance(request);
        var rng = new Random();
        var basePrice = (decimal)(distance * 1.2 + 2.5);
        var surge = rng.NextDouble() < 0.3 ? 1.0 + rng.NextDouble() * 0.8 : 1.0;

        return new[]
        {
            new PriceQuoteDto(ProviderId, ProviderName, LogoUrl,
                basePrice * (decimal)surge,
                basePrice * (decimal)surge * 1.1m,
                "EUR", rng.Next(3, 12), "Economy", surge, 4.5,
                $"https://m.uber.com/looking", DateTime.UtcNow),

            new PriceQuoteDto(ProviderId, "Uber Comfort", LogoUrl,
                basePrice * (decimal)surge * 1.4m,
                basePrice * (decimal)surge * 1.55m,
                "EUR", rng.Next(3, 10), "Comfort", surge, 4.5,
                $"https://m.uber.com/looking", DateTime.UtcNow),
        };
    }

    private static double CalculateDistance(RideRequestDto r)
    {
        const double R = 6371;
        var dLat = (r.DestinationLat - r.OriginLat) * Math.PI / 180;
        var dLon = (r.DestinationLng - r.OriginLng) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(r.OriginLat * Math.PI / 180) * Math.Cos(r.DestinationLat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private record UberPriceResponse(List<UberPrice>? Prices);
    private record UberPrice(string ProductId, decimal LowEstimate, decimal HighEstimate,
        string CurrencyCode, int Duration, double SurgeMultiplier);
}
