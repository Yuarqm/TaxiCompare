using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TaxiCompare.Application.Interfaces;
using TaxiCompare.Domain.Entities;

namespace TaxiCompare.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly ILogger<WeatherService> _logger;
    private readonly string _apiKey;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private const string BaseUrl = "https://api.openweathermap.org/data/2.5/weather";

    public WeatherService(
        HttpClient http,
        IMemoryCache cache,
        IConfiguration config,
        ILogger<WeatherService> logger)
    {
        _http   = http;
        _cache  = cache;
        _logger = logger;
        _apiKey = config["Weather:ApiKey"] ?? "fb68ac62d89a2096cb33f134cedd832f";
    }

    public async Task<WeatherCondition> GetCurrentWeatherAsync(string city, CancellationToken ct = default)
    {
        var cacheKey = $"weather:{city.ToLowerInvariant()}";

        if (_cache.TryGetValue(cacheKey, out WeatherCondition? cached))
            return cached!;

        try
        {
            var url = $"{BaseUrl}?q={Uri.EscapeDataString(city)}&appid={_apiKey}&units=metric";
            var json = await _http.GetStringAsync(url, ct);
            var condition = ParseResponse(city, json);

            _cache.Set(cacheKey, condition, CacheDuration);

            _logger.LogInformation(
                "[Weather] {City}: {Type}, {Temp:F1}°C, ветер {Wind:F0} км/ч → ×{Multiplier}",
                city, condition.Type, condition.TemperatureCelsius,
                condition.WindSpeedKmh, condition.GetPriceMultiplier());

            return condition;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Weather] Не удалось получить погоду для {City}, используем множитель 1.0", city);
            return WeatherCondition.Create(city, 20, 10, 0, WeatherType.Clear);
        }
    }

    public async Task<decimal> GetWeatherMultiplierAsync(string city, CancellationToken ct = default)
    {
        var condition = await GetCurrentWeatherAsync(city, ct);
        return condition.GetPriceMultiplier();
    }

    private static WeatherCondition ParseResponse(string city, string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root      = doc.RootElement;
        var temp      = root.GetProperty("main").GetProperty("temp").GetDouble();
        var windMs    = root.GetProperty("wind").GetProperty("speed").GetDouble();
        var windKmh   = windMs * 3.6;
        var weatherId = root.GetProperty("weather")[0].GetProperty("id").GetInt32();

        var precip = 0.0;
        if (root.TryGetProperty("rain", out var rain) && rain.TryGetProperty("1h", out var r1h))
            precip = r1h.GetDouble();
        else if (root.TryGetProperty("snow", out var snow) && snow.TryGetProperty("1h", out var s1h))
            precip = s1h.GetDouble();

        return WeatherCondition.Create(city, temp, windKmh, precip, MapId(weatherId));
    }

    private static WeatherType MapId(int id) => id switch
    {
        >= 200 and < 300  => WeatherType.Thunderstorm,
        >= 300 and < 400  => WeatherType.Rain,
        >= 500 and < 511  => WeatherType.Rain,
        511               => WeatherType.Snow,
        >= 520 and < 600  => WeatherType.Rain,
        >= 600 and < 620  => WeatherType.Snow,
        620 or 621 or 622 => WeatherType.Blizzard,
        >= 700 and < 800  => WeatherType.Fog,
        800               => WeatherType.Clear,
        _                 => WeatherType.Cloudy
    };
}
