namespace TaxiCompare.Domain.Entities;

public class WeatherCondition
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string City { get; private set; } = default!;
    public double TemperatureCelsius { get; private set; }
    public double WindSpeedKmh { get; private set; }
    public double PrecipitationMm { get; private set; }
    public WeatherType Type { get; private set; }
    public DateTime FetchedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>
    /// Коэффициент наценки: 1.0 = без изменений, 1.3 = +30%
    /// </summary>
    public decimal GetPriceMultiplier()
    {
        decimal multiplier = 1.0m;

        if (Type == WeatherType.Rain)
            multiplier += PrecipitationMm > 10 ? 0.20m : 0.10m;

        if (Type is WeatherType.Snow or WeatherType.Blizzard)
            multiplier += 0.25m;

        if (Type == WeatherType.Thunderstorm)
            multiplier += 0.30m;

        if (WindSpeedKmh > 60)
            multiplier += 0.15m;

        if (TemperatureCelsius > 38 || TemperatureCelsius < -15)
            multiplier += 0.10m;

        return Math.Min(multiplier, 2.0m);
    }

    public string GetConditionRu() => Type switch
    {
        WeatherType.Clear        => "Ясно",
        WeatherType.Cloudy       => "Облачно",
        WeatherType.Rain         => "Дождь",
        WeatherType.Snow         => "Снег",
        WeatherType.Thunderstorm => "Гроза",
        WeatherType.Blizzard     => "Метель",
        WeatherType.Fog          => "Туман",
        _                        => "Неизвестно"
    };

    public static WeatherCondition Create(
        string city,
        double temperatureCelsius,
        double windSpeedKmh,
        double precipitationMm,
        WeatherType type) => new()
    {
        City               = city,
        TemperatureCelsius = temperatureCelsius,
        WindSpeedKmh       = windSpeedKmh,
        PrecipitationMm    = precipitationMm,
        Type               = type
    };
}

public enum WeatherType
{
    Clear,
    Cloudy,
    Rain,
    Snow,
    Thunderstorm,
    Blizzard,
    Fog
}
