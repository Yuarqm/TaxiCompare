using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxiCompare.Domain.Entities;

namespace TaxiCompare.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.FirstName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.LastName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.OwnsOne(u => u.Preferences, p =>
        {
            p.Property(x => x.PreferredCurrency).HasMaxLength(3);
            p.Property(x => x.PriceAlertThreshold).HasPrecision(18, 2);
        });
        builder.HasMany(u => u.RideRequests).WithOne(r => r.User).HasForeignKey(r => r.UserId);
        builder.HasMany(u => u.Notifications).WithOne(n => n.User).HasForeignKey(n => n.UserId);
    }
}

public class RideRequestConfiguration : IEntityTypeConfiguration<RideRequest>
{
    public void Configure(EntityTypeBuilder<RideRequest> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.OriginAddress).IsRequired().HasMaxLength(500);
        builder.Property(r => r.DestinationAddress).IsRequired().HasMaxLength(500);
        builder.HasMany(r => r.PriceSnapshots).WithOne(p => p.RideRequest).HasForeignKey(p => p.RideRequestId);

        // Поля заказа (nullable — заполняются только при нажатии «Заказать»)
        builder.Property(r => r.OrderedProviderName).HasMaxLength(100);
        builder.Property(r => r.OrderedProviderSlug).HasMaxLength(50);
        builder.Property(r => r.OrderedVehicleClass).HasMaxLength(50);
        builder.Property(r => r.OrderedPrice).HasPrecision(18, 2);
    }
}

public class PriceSnapshotConfiguration : IEntityTypeConfiguration<PriceSnapshot>
{
    public void Configure(EntityTypeBuilder<PriceSnapshot> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3);
        builder.Property(p => p.VehicleClass).HasMaxLength(50);
        builder.HasIndex(p => new { p.RideRequestId, p.ProviderId, p.RecordedAt });
    }
}

public class ProviderConfiguration : IEntityTypeConfiguration<Provider>
{
    public void Configure(EntityTypeBuilder<Provider> builder)
    {
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(50);
        builder.HasIndex(p => p.Slug).IsUnique();
    }
}
