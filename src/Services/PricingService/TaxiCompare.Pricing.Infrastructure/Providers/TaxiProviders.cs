using Microsoft.Extensions.Logging;
using TaxiCompare.SharedContracts.DTOs;

namespace TaxiCompare.Pricing.Infrastructure.Providers;

// ─── Bolt Provider ────────────────────────────────────────────────────────────

public class BoltProvider : BaseTaxiProvider
{
    public override string ProviderId => "bolt";
    public override string ProviderName => "Bolt";
    public override string LogoUrl => "https://cdn.taxicompare.app/logos/bolt.svg";

    public BoltProvider(HttpClient httpClient, ILogger<BoltProvider> logger)
        : base(httpClient, logger) { }

    public override async Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        var result = await ExecuteWithResilienceAsync(async _ =>
            await Task.FromResult(SimulatePrices(request)), ct);
        return result ?? Array.Empty<PriceQuoteDto>();
    }

    private IReadOnlyList<PriceQuoteDto> SimulatePrices(RideRequestDto r)
    {
        var distance = CalcDist(r);
        var rng = new Random();
        var basePrice = (decimal)(distance * 1.05 + 1.8); // Bolt generally cheaper
        var surge = rng.NextDouble() < 0.2 ? 1.0 + rng.NextDouble() * 0.5 : 1.0;

        return new[]
        {
            new PriceQuoteDto("bolt", "Bolt", LogoUrl,
                basePrice * (decimal)surge, basePrice * (decimal)surge * 1.08m,
                "EUR", rng.Next(3, 10), "Economy", surge, 4.4,
                $"https://bolt.eu/r/taxi?pickup_lat={r.OriginLat}&pickup_lng={r.OriginLng}", DateTime.UtcNow),

            new PriceQuoteDto("bolt", "Bolt Business", LogoUrl,
                basePrice * (decimal)surge * 1.6m, basePrice * (decimal)surge * 1.75m,
                "EUR", rng.Next(2, 9), "Business", surge, 4.4,
                $"https://bolt.eu/r/taxi", DateTime.UtcNow),
        };
    }

    private static double CalcDist(RideRequestDto r)
    {
        const double R = 6371;
        var dLat = (r.DestinationLat - r.OriginLat) * Math.PI / 180;
        var dLon = (r.DestinationLng - r.OriginLng) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(r.OriginLat * Math.PI / 180) * Math.Cos(r.DestinationLat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

// ─── Yandex Go Provider ───────────────────────────────────────────────────────

public class YandexProvider : BaseTaxiProvider
{
    public override string ProviderId => "yandex";
    public override string ProviderName => "Яндекс Go";
    public override string LogoUrl => "https://cdn.taxicompare.app/logos/yandex.svg";

    public YandexProvider(HttpClient httpClient, ILogger<YandexProvider> logger)
        : base(httpClient, logger) { }

    public override async Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        var result = await ExecuteWithResilienceAsync(async _ =>
            await Task.FromResult(SimulatePrices(request)), ct);
        return result ?? Array.Empty<PriceQuoteDto>();
    }

    private IReadOnlyList<PriceQuoteDto> SimulatePrices(RideRequestDto r)
    {
        var distance = CalcDist(r);
        var rng = new Random();
        var basePrice = (decimal)(distance * 1.15 + 2.0);
        var surge = rng.NextDouble() < 0.25 ? 1.0 + rng.NextDouble() * 1.2 : 1.0;

        return new[]
        {
            new PriceQuoteDto("yandex", "Яндекс Go Эконом", LogoUrl,
                basePrice * (decimal)surge, basePrice * (decimal)surge * 1.05m,
                "EUR", rng.Next(2, 9), "Economy", surge, 4.6,
                $"https://taxi.yandex.ru/", DateTime.UtcNow),

            new PriceQuoteDto("yandex", "Яндекс Go Комфорт", LogoUrl,
                basePrice * (decimal)surge * 1.35m, basePrice * (decimal)surge * 1.45m,
                "EUR", rng.Next(2, 8), "Comfort", surge, 4.6,
                $"https://taxi.yandex.ru/", DateTime.UtcNow),

            new PriceQuoteDto("yandex", "Яндекс Go Бизнес", LogoUrl,
                basePrice * (decimal)surge * 2.1m, basePrice * (decimal)surge * 2.3m,
                "EUR", rng.Next(4, 12), "Business", surge, 4.6,
                $"https://taxi.yandex.ru/", DateTime.UtcNow),
        };
    }

    private static double CalcDist(RideRequestDto r)
    {
        const double R = 6371;
        var dLat = (r.DestinationLat - r.OriginLat) * Math.PI / 180;
        var dLon = (r.DestinationLng - r.OriginLng) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(r.OriginLat * Math.PI / 180) * Math.Cos(r.DestinationLat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

// ─── FreeNow Provider ─────────────────────────────────────────────────────────

public class FreeNowProvider : BaseTaxiProvider
{
    public override string ProviderId => "freenow";
    public override string ProviderName => "FREE NOW";
    public override string LogoUrl => "https://cdn.taxicompare.app/logos/freenow.svg";

    public FreeNowProvider(HttpClient httpClient, ILogger<FreeNowProvider> logger)
        : base(httpClient, logger) { }

    public override async Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        var result = await ExecuteWithResilienceAsync(async _ =>
            await Task.FromResult(SimulatePrices(request)), ct);
        return result ?? Array.Empty<PriceQuoteDto>();
    }

    private IReadOnlyList<PriceQuoteDto> SimulatePrices(RideRequestDto r)
    {
        var distance = CalcDist(r);
        var rng = new Random();
        var basePrice = (decimal)(distance * 1.3 + 3.5); // Licensed taxis, usually higher
        var surge = 1.0; // FreeNow licensed taxis don't surge as much

        return new[]
        {
            new PriceQuoteDto("freenow", "FREE NOW Taxi", LogoUrl,
                basePrice, basePrice * 1.2m,
                "EUR", rng.Next(4, 15), "Taxi", surge, 4.3,
                $"https://free-now.com/de/", DateTime.UtcNow),
        };
    }

    private static double CalcDist(RideRequestDto r)
    {
        const double R = 6371;
        var dLat = (r.DestinationLat - r.OriginLat) * Math.PI / 180;
        var dLon = (r.DestinationLng - r.OriginLng) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(r.OriginLat * Math.PI / 180) * Math.Cos(r.DestinationLat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}

// ─── Lyft Provider ────────────────────────────────────────────────────────────

public class LyftProvider : BaseTaxiProvider
{
    public override string ProviderId => "lyft";
    public override string ProviderName => "Lyft";
    public override string LogoUrl => "https://cdn.taxicompare.app/logos/lyft.svg";

    private static readonly HashSet<string> SupportedRegions = new() { "US", "CA" };

    public LyftProvider(HttpClient httpClient, ILogger<LyftProvider> logger)
        : base(httpClient, logger) { }

    public override async Task<bool> IsAvailableInRegionAsync(double lat, double lng, CancellationToken ct = default)
    {
        // Lyft only available in North America
        return await Task.FromResult(lat is > 24 and < 72 && lng is > -170 and < -52);
    }

    public override async Task<IReadOnlyList<PriceQuoteDto>> GetQuotesAsync(
        RideRequestDto request, CancellationToken ct = default)
    {
        var available = await IsAvailableInRegionAsync(request.OriginLat, request.OriginLng, ct);
        if (!available) return Array.Empty<PriceQuoteDto>();

        var result = await ExecuteWithResilienceAsync(async _ =>
            await Task.FromResult(SimulatePrices(request)), ct);
        return result ?? Array.Empty<PriceQuoteDto>();
    }

    private IReadOnlyList<PriceQuoteDto> SimulatePrices(RideRequestDto r)
    {
        var distance = CalcDist(r);
        var rng = new Random();
        var basePrice = (decimal)(distance * 1.1 + 2.0);
        var surge = rng.NextDouble() < 0.2 ? 1.0 + rng.NextDouble() * 0.6 : 1.0;

        return new[]
        {
            new PriceQuoteDto("lyft", "Lyft", LogoUrl,
                basePrice * (decimal)surge, basePrice * (decimal)surge * 1.1m,
                "USD", rng.Next(3, 10), "Economy", surge, 4.4,
                $"https://lyft.com/ride", DateTime.UtcNow),
        };
    }

    private static double CalcDist(RideRequestDto r)
    {
        const double R = 6371;
        var dLat = (r.DestinationLat - r.OriginLat) * Math.PI / 180;
        var dLon = (r.DestinationLng - r.OriginLng) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(r.OriginLat * Math.PI / 180) * Math.Cos(r.DestinationLat * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
