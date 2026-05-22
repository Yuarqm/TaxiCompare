namespace TaxiCompare.Pricing.Domain.Entities;

public class Provider
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string LogoUrl { get; private set; } = default!;
    public string ApiBaseUrl { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public double AverageRating { get; private set; }
    public ICollection<PriceSnapshot> PriceSnapshots { get; private set; } = new List<PriceSnapshot>();

    private Provider() { }

    public static Provider Create(string name, string slug, string logoUrl, string apiBaseUrl)
    {
        return new Provider
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            LogoUrl = logoUrl,
            ApiBaseUrl = apiBaseUrl,
            IsActive = true,
            AverageRating = 4.5
        };
    }

    public void Deactivate() => IsActive = false;
    public void UpdateRating(double rating) => AverageRating = rating;
}

public class RideRequest
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public string Origin { get; private set; } = default!;
    public double OriginLat { get; private set; }
    public double OriginLng { get; private set; }
    public string Destination { get; private set; } = default!;
    public double DestinationLat { get; private set; }
    public double DestinationLng { get; private set; }
    public string? PreferredClass { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public ICollection<PriceSnapshot> Snapshots { get; private set; } = new List<PriceSnapshot>();

    private RideRequest() { }

    public static RideRequest Create(string userId, string origin, double originLat, double originLng,
        string destination, double destLat, double destLng, string? preferredClass = null)
    {
        return new RideRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Origin = origin,
            OriginLat = originLat,
            OriginLng = originLng,
            Destination = destination,
            DestinationLat = destLat,
            DestinationLng = destLng,
            PreferredClass = preferredClass,
            CreatedAt = DateTime.UtcNow
        };
    }
}

public class PriceSnapshot
{
    public Guid Id { get; private set; }
    public Guid RideRequestId { get; private set; }
    public RideRequest RideRequest { get; private set; } = default!;
    public Guid ProviderId { get; private set; }
    public Provider Provider { get; private set; } = default!;
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public int EtaMinutes { get; private set; }
    public string VehicleClass { get; private set; } = default!;
    public double SurgeMultiplier { get; private set; }
    public string DeepLinkUrl { get; private set; } = default!;
    public DateTime CapturedAt { get; private set; }

    private PriceSnapshot() { }

    public static PriceSnapshot Create(Guid rideRequestId, Guid providerId,
        decimal minPrice, decimal maxPrice, string currency,
        int etaMinutes, string vehicleClass, double surgeMultiplier, string deepLinkUrl)
    {
        return new PriceSnapshot
        {
            Id = Guid.NewGuid(),
            RideRequestId = rideRequestId,
            ProviderId = providerId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            Currency = currency,
            EtaMinutes = etaMinutes,
            VehicleClass = vehicleClass,
            SurgeMultiplier = surgeMultiplier,
            DeepLinkUrl = deepLinkUrl,
            CapturedAt = DateTime.UtcNow
        };
    }
}

public class UserPreference
{
    public Guid Id { get; private set; }
    public string UserId { get; private set; } = default!;
    public string PreferredCurrency { get; private set; } = "EUR";
    public bool EmailNotifications { get; private set; }
    public bool PushNotifications { get; private set; }
    public decimal? PriceAlertThreshold { get; private set; }
    public IList<string> FavoriteProviders { get; private set; } = new List<string>();

    private UserPreference() { }

    public static UserPreference CreateDefault(string userId) =>
        new UserPreference
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PreferredCurrency = "EUR",
            EmailNotifications = true,
            PushNotifications = false
        };
}
