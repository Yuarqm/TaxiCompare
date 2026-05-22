using Microsoft.EntityFrameworkCore;
using TaxiCompare.Domain.Entities;
using TaxiCompare.Domain.Interfaces;
using TaxiCompare.Infrastructure.Persistence;

namespace TaxiCompare.Infrastructure.Repositories;

public abstract class BaseRepository<T> : IRepository<T> where T : class
{
    protected readonly TaxiCompareDbContext _context;
    protected BaseRepository(TaxiCompareDbContext context) => _context = context;

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _context.Set<T>().FindAsync(new object[] { id }, ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await _context.Set<T>().ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default) =>
        await _context.Set<T>().AddAsync(entity, ct);

    public void Update(T entity) => _context.Set<T>().Update(entity);
    public void Remove(T entity) => _context.Set<T>().Remove(entity);

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(TaxiCompareDbContext context) : base(context) { }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant(), ct);

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default) =>
        await _context.Users.AnyAsync(u => u.Email == email.ToLowerInvariant(), ct);
}

public class RideRequestRepository : BaseRepository<RideRequest>, IRideRequestRepository
{
    public RideRequestRepository(TaxiCompareDbContext context) : base(context) { }

    public async Task<IEnumerable<RideRequest>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        await _context.RideRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RequestedAt)
            .Include(r => r.PriceSnapshots)
            .ToListAsync(ct);

    public async Task<IEnumerable<RideRequest>> GetPopularRoutesAsync(int count = 10, CancellationToken ct = default) =>
        await _context.RideRequests
            .GroupBy(r => new { r.OriginAddress, r.DestinationAddress })
            .OrderByDescending(g => g.Count())
            .Take(count)
            .Select(g => g.First())
            .ToListAsync(ct);
}

public class PriceSnapshotRepository : BaseRepository<PriceSnapshot>, IPriceSnapshotRepository
{
    public PriceSnapshotRepository(TaxiCompareDbContext context) : base(context) { }

    public async Task<IEnumerable<PriceSnapshot>> GetByRideRequestAsync(Guid rideRequestId, CancellationToken ct = default) =>
        await _context.PriceSnapshots
            .Include(p => p.Provider)
            .Where(p => p.RideRequestId == rideRequestId)
            .ToListAsync(ct);

    public async Task<IEnumerable<PriceSnapshot>> GetHistoryAsync(Guid providerId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _context.PriceSnapshots
            .Where(p => p.ProviderId == providerId && p.RecordedAt >= from && p.RecordedAt <= to)
            .OrderBy(p => p.RecordedAt)
            .ToListAsync(ct);

    public async Task<IEnumerable<PriceSnapshot>> GetLatestForRouteAsync(double originLat, double originLng,
        double destLat, double destLng, CancellationToken ct = default)
    {
        var rideRequestIds = await _context.RideRequests
            .Where(r => Math.Abs(r.OriginLat - originLat) < 0.001 && Math.Abs(r.OriginLng - originLng) < 0.001
                     && Math.Abs(r.DestinationLat - destLat) < 0.001 && Math.Abs(r.DestinationLng - destLng) < 0.001)
            .Select(r => r.Id)
            .ToListAsync(ct);

        return await _context.PriceSnapshots
            .Include(p => p.Provider)
            .Where(p => rideRequestIds.Contains(p.RideRequestId))
            .OrderByDescending(p => p.RecordedAt)
            .Take(50)
            .ToListAsync(ct);
    }
}

public class ProviderRepository : BaseRepository<Provider>, IProviderRepository
{
    public ProviderRepository(TaxiCompareDbContext context) : base(context) { }

    public async Task<Provider?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        await _context.Providers.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public async Task<IEnumerable<Provider>> GetActiveAsync(CancellationToken ct = default) =>
        await _context.Providers.Where(p => p.IsActive).ToListAsync(ct);
}
