using TaxiCompare.Domain.Enums;

namespace TaxiCompare.Domain.Entities;

public class RideRequest
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string OriginAddress { get; private set; } = string.Empty;
    public double OriginLat { get; private set; }
    public double OriginLng { get; private set; }
    public string DestinationAddress { get; private set; } = string.Empty;
    public double DestinationLat { get; private set; }
    public double DestinationLng { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public RideStatus Status { get; private set; }
    public ICollection<PriceSnapshot> PriceSnapshots { get; private set; } = new List<PriceSnapshot>();

    // Заполняется когда пользователь нажимает «Заказать»
    public string? OrderedProviderName { get; private set; }
    public string? OrderedProviderSlug { get; private set; }
    public string? OrderedVehicleClass { get; private set; }
    public decimal? OrderedPrice { get; private set; }
    public DateTime? OrderedAt { get; private set; }

    private RideRequest() { }

    public static RideRequest Create(Guid userId, string originAddress, double originLat, double originLng,
        string destAddress, double destLat, double destLng)
    {
        return new RideRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            OriginAddress = originAddress,
            OriginLat = originLat,
            OriginLng = originLng,
            DestinationAddress = destAddress,
            DestinationLat = destLat,
            DestinationLng = destLng,
            RequestedAt = DateTime.UtcNow,
            Status = RideStatus.Pending
        };
    }

    public void Complete() => Status = RideStatus.Completed;
    public void Cancel() => Status = RideStatus.Cancelled;

    public void PlaceOrder(string providerName, string providerSlug, string vehicleClass, decimal price)
    {
        OrderedProviderName = providerName;
        OrderedProviderSlug = providerSlug;
        OrderedVehicleClass = vehicleClass;
        OrderedPrice = price;
        OrderedAt = DateTime.UtcNow;
        Status = RideStatus.Ordered;
    }
}

public class PriceSnapshot
{
    public Guid Id { get; private set; }
    public Guid RideRequestId { get; private set; }
    public RideRequest RideRequest { get; private set; } = null!;
    public Guid ProviderId { get; private set; }
    public Provider Provider { get; private set; } = null!;
    public decimal Price { get; private set; }
    public string Currency { get; private set; } = "EUR";
    public int EtaMinutes { get; private set; }
    public string VehicleClass { get; private set; } = string.Empty;
    public double SurgeMultiplier { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public bool IsAvailable { get; private set; }

    private PriceSnapshot() { }

    public static PriceSnapshot Create(Guid rideRequestId, Guid providerId, decimal price,
        string currency, int eta, string vehicleClass, double surge)
    {
        return new PriceSnapshot
        {
            Id = Guid.NewGuid(),
            RideRequestId = rideRequestId,
            ProviderId = providerId,
            Price = price,
            Currency = currency,
            EtaMinutes = eta,
            VehicleClass = vehicleClass,
            SurgeMultiplier = surge,
            RecordedAt = DateTime.UtcNow,
            IsAvailable = true
        };
    }
}

public class Provider
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string LogoUrl { get; private set; } = string.Empty;
    public double Rating { get; private set; }
    public bool IsActive { get; private set; }
    public ICollection<PriceSnapshot> PriceSnapshots { get; private set; } = new List<PriceSnapshot>();

    private Provider() { }

    public static Provider Create(string name, string slug, string logoUrl, double rating = 4.5)
    {
        return new Provider
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            LogoUrl = logoUrl,
            Rating = rating,
            IsActive = true
        };
    }
}

public class Notification
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public NotificationType Type { get; private set; }
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Notification() { }

    public static Notification Create(Guid userId, string title, string message, NotificationType type)
    {
        return new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkRead() => IsRead = true;
}

public class UserPreferences
{
    public string PreferredCurrency { get; set; } = "EUR";
    public bool PriceAlertEnabled { get; set; } = true;
    public decimal PriceAlertThreshold { get; set; } = 5.00m;
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    public string PreferredVehicleClass { get; set; } = "Economy";

    public static UserPreferences Default() => new();
}
