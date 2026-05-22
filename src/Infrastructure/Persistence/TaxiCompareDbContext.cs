using Microsoft.EntityFrameworkCore;
using TaxiCompare.Domain.Entities;

namespace TaxiCompare.Infrastructure.Persistence;

public class TaxiCompareDbContext : DbContext
{
    public TaxiCompareDbContext(DbContextOptions<TaxiCompareDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<RideRequest> RideRequests => Set<RideRequest>();
    public DbSet<PriceSnapshot> PriceSnapshots => Set<PriceSnapshot>();
    public DbSet<Provider> Providers => Set<Provider>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaxiCompareDbContext).Assembly);
    }
}
