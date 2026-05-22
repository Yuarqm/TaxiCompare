using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TaxiCompare.Application.DTOs;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Infrastructure.Services;

namespace TaxiCompare.Infrastructure.Providers;

public abstract class BaseTaxiProvider : ITaxiProvider
{
    protected readonly HttpClient _http;
    protected readonly ILogger _logger;

    protected BaseTaxiProvider(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    public abstract string ProviderName { get; }
    public abstract string ProviderSlug { get; }
    public abstract bool IsAvailableInRegion(double lat, double lng);
    public abstract Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken ct = default);

    protected ProviderPriceDto UnavailableResult(Guid providerId, string name, string slug, string logo) =>
        new(providerId, name, slug, logo, 0, "RUB", 0, "N/A", 1.0, 4.5, false, false);

    protected static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLon = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    protected static double GetDistanceKm(PriceComparisonRequest req)
        => req.DistanceKm ?? CalculateDistance(req.OriginLat, req.OriginLng, req.DestinationLat, req.DestinationLng) * 1.35;

    /// <summary>Ночная надбавка: 23:00-06:00</summary>
    protected static double NightSurge(double nightMultiplier)
    {
        var hour = DateTime.Now.Hour;
        return (hour >= 23 || hour < 6) ? nightMultiplier : 1.0;
    }

    /// <summary>Совпадает ли класс с фильтром пользователя</summary>
    protected static bool MatchesClass(string vehicleClass, string? preferred)
        => string.IsNullOrEmpty(preferred) || preferred == vehicleClass;
}

// Яндекс Такси
public class YandexProvider : BaseTaxiProvider
{
    private static readonly Guid IdEconomy  = Guid.Parse("22222222-2222-2222-2222-222222222201");
    private static readonly Guid IdComfort  = Guid.Parse("22222222-2222-2222-2222-222222222202");
    private static readonly Guid IdBusiness = Guid.Parse("22222222-2222-2222-2222-222222222203");
    public override string ProviderName => "Яндекс Такси";
    public override string ProviderSlug => "yandex";

    public YandexProvider(HttpClient http, ILogger<YandexProvider> logger) : base(http, logger) { }

    public override bool IsAvailableInRegion(double lat, double lng) =>
        lat >= 40 && lat <= 78 && lng >= 20 && lng <= 180;

    public override async Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken ct = default)
    {
        if (!IsAvailableInRegion(request.OriginLat, request.OriginLng))
            return UnavailableResult(IdEconomy, ProviderName, ProviderSlug, "/logos/yandex.svg");

        await Task.CompletedTask;
        var dist = GetDistanceKm(request);
        var preferred = request.PreferredClass;
        var night = NightSurge(1.3);
        var surge = Random.Shared.NextDouble() < 0.2 ? Math.Round(1.1 + Random.Shared.NextDouble() * 0.4, 1) : 1.0;
        var totalSurge = (decimal)(surge * night);

        if (MatchesClass("Economy", preferred))
        {
            var eta = (int)(dist / 40.0 * 60 + Random.Shared.Next(3, 10));
            var price = Math.Max(199, Math.Round((decimal)(99 + dist * 15 + eta * 4) * totalSurge, 0));
            return new(IdEconomy, ProviderName, ProviderSlug, "/logos/yandex.svg",
                price, "RUB", eta, "Economy", surge * night, 4.6, true, false);
        }
        if (MatchesClass("Comfort", preferred))
        {
            var eta = (int)(dist / 38.0 * 60 + Random.Shared.Next(4, 12));
            var price = Math.Max(299, Math.Round((decimal)(149 + dist * 22 + eta * 6) * totalSurge, 0));
            return new(IdComfort, ProviderName, ProviderSlug, "/logos/yandex.svg",
                price, "RUB", eta, "Comfort", surge * night, 4.7, true, false);
        }
        if (MatchesClass("Business", preferred))
        {
            var eta = (int)(dist / 35.0 * 60 + Random.Shared.Next(5, 15));
            var price = Math.Max(499, Math.Round((decimal)(249 + dist * 35 + eta * 9) * totalSurge, 0));
            return new(IdBusiness, ProviderName, ProviderSlug, "/logos/yandex.svg",
                price, "RUB", eta, "Business", surge * night, 4.8, true, false);
        }
        return null;
    }
}

// Uber
public class UberProvider : BaseTaxiProvider
{
    private static readonly Guid IdEconomy  = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid IdComfort  = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid IdBusiness = Guid.Parse("11111111-1111-1111-1111-111111111103");
    public override string ProviderName => "Uber";
    public override string ProviderSlug => "uber";

    public UberProvider(HttpClient http, ILogger<UberProvider> logger) : base(http, logger) { }

    public override bool IsAvailableInRegion(double lat, double lng) => true;

    public override async Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        var dist = GetDistanceKm(request);
        var preferred = request.PreferredClass;
        var night = NightSurge(1.5);
        var surge = Random.Shared.NextDouble() < 0.25 ? Math.Round(1.2 + Random.Shared.NextDouble() * 0.6, 1) : 1.0;
        var totalSurge = (decimal)(surge * night);

        if (MatchesClass("Economy", preferred))
        {
            var eta = (int)(dist / 38.0 * 60 + Random.Shared.Next(4, 12));
            var price = Math.Max(219, Math.Round((decimal)(109 + dist * 17 + eta * 5) * totalSurge, 0));
            return new(IdEconomy, ProviderName, ProviderSlug, "/logos/uber.svg",
                price, "RUB", eta, "Economy", surge * night, 4.7, true, false);
        }
        if (MatchesClass("Comfort", preferred))
        {
            var eta = (int)(dist / 36.0 * 60 + Random.Shared.Next(5, 13));
            var price = Math.Max(349, Math.Round((decimal)(179 + dist * 26 + eta * 7) * totalSurge, 0));
            return new(IdComfort, ProviderName, ProviderSlug, "/logos/uber.svg",
                price, "RUB", eta, "Comfort", surge * night, 4.8, true, false);
        }
        if (MatchesClass("Business", preferred))
        {
            var eta = (int)(dist / 33.0 * 60 + Random.Shared.Next(6, 16));
            var price = Math.Max(599, Math.Round((decimal)(299 + dist * 42 + eta * 11) * totalSurge, 0));
            return new(IdBusiness, ProviderName, ProviderSlug, "/logos/uber.svg",
                price, "RUB", eta, "Business", surge * night, 4.9, true, false);
        }
        return null;
    }
}

// Такси Максим (только Эконом и Комфорт, нет Бизнеса)
public class FreeNowProvider : BaseTaxiProvider
{
    private static readonly Guid IdEconomy = Guid.Parse("44444444-4444-4444-4444-444444444401");
    private static readonly Guid IdComfort = Guid.Parse("44444444-4444-4444-4444-444444444402");
    public override string ProviderName => "Такси Максим";
    public override string ProviderSlug => "maksim";

    public FreeNowProvider(HttpClient http, ILogger<FreeNowProvider> logger) : base(http, logger) { }

    public override bool IsAvailableInRegion(double lat, double lng) =>
        lat >= 40 && lat <= 78 && lng >= 20 && lng <= 180;

    public override async Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken ct = default)
    {
        if (!IsAvailableInRegion(request.OriginLat, request.OriginLng))
            return UnavailableResult(IdEconomy, ProviderName, ProviderSlug, "/logos/maksim.svg");

        await Task.CompletedTask;
        var dist = GetDistanceKm(request);
        var preferred = request.PreferredClass;
        var night = NightSurge(1.15);

        // Максим не имеет класса Бизнес
        if (preferred == "Business") return null;

        if (MatchesClass("Economy", preferred))
        {
            var eta = (int)(dist / 35.0 * 60 + Random.Shared.Next(5, 15));
            var price = Math.Max(149, Math.Round((decimal)(69 + dist * 12 + eta * 3) * (decimal)night, 0));
            return new(IdEconomy, ProviderName, ProviderSlug, "/logos/maksim.svg",
                price, "RUB", eta, "Economy", night, 4.3, true, false);
        }
        if (MatchesClass("Comfort", preferred))
        {
            var eta = (int)(dist / 33.0 * 60 + Random.Shared.Next(6, 14));
            var price = Math.Max(249, Math.Round((decimal)(119 + dist * 18 + eta * 5) * (decimal)night, 0));
            return new(IdComfort, ProviderName, ProviderSlug, "/logos/maksim.svg",
                price, "RUB", eta, "Comfort", night, 4.4, true, false);
        }
        return null;
    }
}

// Омега Такси
public class BoltProvider : BaseTaxiProvider
{
    private static readonly Guid IdEconomy  = Guid.Parse("33333333-3333-3333-3333-333333333301");
    private static readonly Guid IdComfort  = Guid.Parse("33333333-3333-3333-3333-333333333302");
    private static readonly Guid IdBusiness = Guid.Parse("33333333-3333-3333-3333-333333333303");
    public override string ProviderName => "Омега";
    public override string ProviderSlug => "omega";

    public BoltProvider(HttpClient http, ILogger<BoltProvider> logger) : base(http, logger) { }

    public override bool IsAvailableInRegion(double lat, double lng) =>
        lat >= 40 && lat <= 78 && lng >= 20 && lng <= 180;

    public override async Task<ProviderPriceDto?> GetPriceAsync(PriceComparisonRequest request, CancellationToken ct = default)
    {
        if (!IsAvailableInRegion(request.OriginLat, request.OriginLng))
            return UnavailableResult(IdEconomy, ProviderName, ProviderSlug, "/logos/omega.svg");

        await Task.CompletedTask;
        var dist = GetDistanceKm(request);
        var preferred = request.PreferredClass;
        var night = NightSurge(1.2);
        var surge = Random.Shared.NextDouble() < 0.15 ? Math.Round(1.1 + Random.Shared.NextDouble() * 0.3, 1) : 1.0;
        var totalSurge = (decimal)(surge * night);

        if (MatchesClass("Economy", preferred))
        {
            var eta = (int)(dist / 37.0 * 60 + Random.Shared.Next(3, 9));
            var price = Math.Max(179, Math.Round((decimal)(89 + dist * 14 + eta * 4) * totalSurge, 0));
            return new(IdEconomy, ProviderName, ProviderSlug, "/logos/omega.svg",
                price, "RUB", eta, "Economy", surge * night, 4.4, true, false);
        }
        if (MatchesClass("Comfort", preferred))
        {
            var eta = (int)(dist / 35.0 * 60 + Random.Shared.Next(4, 10));
            var price = Math.Max(279, Math.Round((decimal)(139 + dist * 20 + eta * 6) * totalSurge, 0));
            return new(IdComfort, ProviderName, ProviderSlug, "/logos/omega.svg",
                price, "RUB", eta, "Comfort", surge * night, 4.5, true, false);
        }
        if (MatchesClass("Business", preferred))
        {
            var eta = (int)(dist / 32.0 * 60 + Random.Shared.Next(5, 12));
            var price = Math.Max(449, Math.Round((decimal)(219 + dist * 32 + eta * 8) * totalSurge, 0));
            return new(IdBusiness, ProviderName, ProviderSlug, "/logos/omega.svg",
                price, "RUB", eta, "Business", surge * night, 4.6, true, false);
        }
        return null;
    }
}

// Aggregator
public class PricingAggregator : IPricingAggregator
{
    private readonly IEnumerable<ITaxiProvider> _providers;
    private readonly IWeatherService _weather;
    private readonly ILogger<PricingAggregator> _logger;

    public PricingAggregator(
        IEnumerable<ITaxiProvider> providers,
        IWeatherService weather,
        ILogger<PricingAggregator> logger)
    {
        _providers = providers;
        _weather   = weather;
        _logger    = logger;
    }

    public async Task<PriceComparisonResult> GetAllPricesAsync(PriceComparisonRequest request, CancellationToken ct = default)
    {
        var city = request.OriginCity ?? ResolveCity(request.OriginLat, request.OriginLng);
        var weatherMultiplier = await _weather.GetWeatherMultiplierAsync(city, ct);

        // Всегда запрашиваем все классы — фильтрация на клиенте
        var classes = new[] { "Economy", "Comfort", "Business" };
        var allTasks = _providers
            .Where(p => p.IsAvailableInRegion(request.OriginLat, request.OriginLng))
            .SelectMany(p => classes.Select(cls => new { Provider = p, Class = cls }))
            .Select(async x =>
            {
                try
                {
                    var req = request with { PreferredClass = x.Class };
                    return await x.Provider.GetPriceAsync(req, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Provider {Name}/{Class} failed", x.Provider.ProviderName, x.Class);
                    return null;
                }
            });

        var results = (await Task.WhenAll(allTasks))
            .Where(r => r is not null)
            .Cast<ProviderPriceDto>()
            .ToList();

        var adjusted = results.Select(r =>
        {
            if (!r.IsAvailable || weatherMultiplier <= 1.0m) return r;
            return r with { Price = Math.Round(r.Price * weatherMultiplier, 0) };
        }).ToList();

        var available = adjusted.Where(r => r.IsAvailable).OrderBy(r => r.Price).ToList();
        var bestDeal  = available.FirstOrDefault();

        var pricesWithBest = adjusted.Select(r =>
            r.IsAvailable && r == bestDeal ? r with { IsBestDeal = true } : r).ToList();

        if (weatherMultiplier > 1.0m)
            _logger.LogInformation(
                "[Pricing] Weather surcharge x{M} applied to {Count} providers for city={City}",
                weatherMultiplier, available.Count, city);

        return new PriceComparisonResult(Guid.Empty, pricesWithBest, bestDeal, DateTime.UtcNow);
    }

    private static string ResolveCity(double lat, double lng) => (lat, lng) switch
    {
        _ when lat is >= 55.0 and <= 56.0 && lng is >= 37.0 and <= 38.0 => "Moscow",
        _ when lat is >= 59.5 and <= 60.5 && lng is >= 29.5 and <= 31.0 => "Saint Petersburg",
        _ when lat is >= 56.5 and <= 57.2 && lng is >= 60.5 and <= 61.5 => "Yekaterinburg",
        _ when lat is >= 55.0 and <= 56.0 && lng is >= 82.5 and <= 83.5 => "Novosibirsk",
        _ when lat is >= 54.5 and <= 55.0 && lng is >= 73.0 and <= 74.0 => "Omsk",
        _ when lat is >= 56.8 and <= 57.2 && lng is >= 69.0 and <= 70.0 => "Tyumen",
        _ => "Moscow"
    };
}
