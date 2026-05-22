using FluentAssertions;
using TaxiCompare.Domain.Entities;
using Xunit;

namespace TaxiCompare.Domain.Tests;

public class UserTests
{
    [Fact]
    public void Create_Should_Set_All_Properties()
    {
        var user = User.Create("Test@Example.com", "hash123", "John", "Doe", "+49123456789");

        user.Email.Should().Be("test@example.com"); // lowercased
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.PhoneNumber.Should().Be("+49123456789");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_Should_Lowercase_Email()
    {
        var user = User.Create("UPPER@CASE.COM", "hash", "A", "B");
        user.Email.Should().Be("upper@case.com");
    }

    [Fact]
    public void UpdateLastLogin_Should_Set_Timestamp()
    {
        var user = User.Create("a@b.com", "hash", "A", "B");
        user.LastLoginAt.Should().BeNull();

        user.UpdateLastLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Deactivate_Should_Set_IsActive_False()
    {
        var user = User.Create("a@b.com", "hash", "A", "B");
        user.IsActive.Should().BeTrue();

        user.Deactivate();

        user.IsActive.Should().BeFalse();
    }
}

public class RideRequestTests
{
    [Fact]
    public void Create_Should_Have_Pending_Status()
    {
        var ride = RideRequest.Create(
            Guid.NewGuid(),
            "Hauptbahnhof, Frankfurt", 50.1071, 8.6640,
            "Frankfurt Airport", 50.0379, 8.5622
        );

        ride.Status.Should().Be(Domain.Enums.RideStatus.Pending);
        ride.Id.Should().NotBeEmpty();
        ride.RequestedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Complete_Should_Change_Status()
    {
        var ride = RideRequest.Create(Guid.NewGuid(), "A", 0, 0, "B", 1, 1);
        ride.Complete();
        ride.Status.Should().Be(Domain.Enums.RideStatus.Completed);
    }

    [Fact]
    public void Cancel_Should_Change_Status()
    {
        var ride = RideRequest.Create(Guid.NewGuid(), "A", 0, 0, "B", 1, 1);
        ride.Cancel();
        ride.Status.Should().Be(Domain.Enums.RideStatus.Cancelled);
    }
}

public class PriceSnapshotTests
{
    [Fact]
    public void Create_Should_Set_All_Fields()
    {
        var rideId = Guid.NewGuid();
        var providerId = Guid.NewGuid();

        var snapshot = PriceSnapshot.Create(rideId, providerId, 12.50m, "EUR", 5, "Economy", 1.2);

        snapshot.RideRequestId.Should().Be(rideId);
        snapshot.ProviderId.Should().Be(providerId);
        snapshot.Price.Should().Be(12.50m);
        snapshot.Currency.Should().Be("EUR");
        snapshot.EtaMinutes.Should().Be(5);
        snapshot.VehicleClass.Should().Be("Economy");
        snapshot.SurgeMultiplier.Should().Be(1.2);
        snapshot.IsAvailable.Should().BeTrue();
        snapshot.RecordedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}

public class NotificationTests
{
    [Fact]
    public void Create_Should_Be_Unread()
    {
        var notif = Notification.Create(Guid.NewGuid(), "Price Drop!", "Uber is now cheaper", Domain.Enums.NotificationType.PriceDrop);
        notif.IsRead.Should().BeFalse();
    }

    [Fact]
    public void MarkRead_Should_Set_IsRead()
    {
        var notif = Notification.Create(Guid.NewGuid(), "Test", "Msg", Domain.Enums.NotificationType.System);
        notif.MarkRead();
        notif.IsRead.Should().BeTrue();
    }
}
