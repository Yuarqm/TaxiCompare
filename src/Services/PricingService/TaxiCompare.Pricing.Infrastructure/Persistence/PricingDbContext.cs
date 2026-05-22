using Microsoft.EntityFrameworkCore;
using TaxiCompare.Pricing.Domain.Entities;

namespace TaxiCompare.Pricing.Infrastructure.Persistence;

public class PricingDbContext : DbContext
{
    public PricingDbContext(DbContextOptions<PricingDbContext> options) : base(options) { }

    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<RideRequest> RideRequests => Set<RideRequest>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<UserPreference> UserPreferences => Set<UserPreference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Provider>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired().HasMaxLength(100);
            e.Property(x => x.Slug).IsRequired().HasMaxLength(50);
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.LogoUrl).HasMaxLength(500);
            e.Property(x => x.ApiBaseUrl).HasMaxLength(500);
        });

        modelBuilder.Entity<RideRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(100);
            e.Property(x => x.Origin).IsRequired().HasMaxLength(500);
            e.Property(x => x.Destination).IsRequired().HasMaxLength(500);
            e.HasIndex(x => x.UserId);
            e.HasIndex(x => x.CreatedAt);
        });

        modelBuilder.Entity<PriceSnapshot>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.MinPrice).HasPrecision(10, 2);
            e.Property(x => x.MaxPrice).HasPrecision(10, 2);
            e.Property(x => x.Currency).HasMaxLength(3);
            e.Property(x => x.VehicleClass).HasMaxLength(50);
            e.Property(x => x.DeepLinkUrl).HasMaxLength(2000);
            e.HasOne(x => x.RideRequest).WithMany(x => x.Snapshots)
                .HasForeignKey(x => x.RideRequestId);
            e.HasOne(x => x.Provider).WithMany(x => x.PriceSnapshots)
                .HasForeignKey(x => x.ProviderId);
            e.HasIndex(x => x.CapturedAt);
        });

        modelBuilder.Entity<UserPreference>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).IsRequired().HasMaxLength(100);
            e.HasIndex(x => x.UserId).IsUnique();
            e.Property(x => x.PriceAlertThreshold).HasPrecision(10, 2);
            e.Property(x => x.FavoriteProviders)
                .HasConversion(
                    v => string.Join(',', v),
                    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());
        });

        // Seed providers
        SeedProviders(modelBuilder);
    }

    private static void SeedProviders(ModelBuilder modelBuilder)
    {
        var providers = new[]
        {
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Uber", Slug = "uber",
                  LogoUrl = "https://cdn.taxicompare.app/logos/uber.svg",
                  ApiBaseUrl = "https://api.uber.com/v1.2", IsActive = true, AverageRating = 4.5 },
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Bolt", Slug = "bolt",
                  LogoUrl = "https://cdn.taxicompare.app/logos/bolt.svg",
                  ApiBaseUrl = "https://node.bolt.eu/booking/taxi/v2", IsActive = true, AverageRating = 4.4 },
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Яндекс Go", Slug = "yandex",
                  LogoUrl = "https://cdn.taxicompare.app/logos/yandex.svg",
                  ApiBaseUrl = "https://taxi-routeinfo.taxi.yandex.net", IsActive = true, AverageRating = 4.6 },
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "FreeNow", Slug = "freenow",
                  LogoUrl = "https://cdn.taxicompare.app/logos/freenow.svg",
                  ApiBaseUrl = "https://api.free-now.com/v1", IsActive = true, AverageRating = 4.3 },
            new { Id = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Lyft", Slug = "lyft",
                  LogoUrl = "https://cdn.taxicompare.app/logos/lyft.svg",
                  ApiBaseUrl = "https://api.lyft.com/v1", IsActive = true, AverageRating = 4.4 },
        };

        foreach (var p in providers)
        {
            modelBuilder.Entity<Provider>().HasData(p);
        }
    }
}
