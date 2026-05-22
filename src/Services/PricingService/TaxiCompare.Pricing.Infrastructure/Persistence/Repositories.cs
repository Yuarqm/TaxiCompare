using Microsoft.EntityFrameworkCore;
using TaxiCompare.Pricing.Domain.Entities;
using TaxiCompare.Pricing.Domain.Interfaces;

namespace TaxiCompare.Pricing.Infrastructure.Persistence;

public class ProviderRepository : IProviderRepository
{
    private readonly PricingDbContext _db;
    public ProviderRepository(PricingDbContext db) => _db = db;

    public Task<IReadOnlyList<Provider>> GetActiveProvidersAsync(CancellationToken ct) =>
        _db.Providers.Where(p => p.IsActive).ToListAsync(ct)
            .ContinueWith(t => (IReadOnlyList<Provider>)t.Result, ct);

    public Task<Provider?> GetBySlugAsync(string slug, CancellationToken ct) =>
        _db.Providers.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.Providers.FindAsync(new object[] { id }, ct).AsTask();

    public async Task AddAsync(Provider provider, CancellationToken ct) =>
        await _db.Providers.AddAsync(provider, ct);

    public Task SaveChangesAsync(CancellationToken ct) =>
        _db.SaveChangesAsync(ct);
}

public class RideRequestRepository : IRideRequestRepository
{
    private readonly PricingDbContext _db;
    public RideRequestRepository(PricingDbContext db) => _db = db;

    public Task<RideRequest?> GetByIdAsync(Guid id, CancellationToken ct) =>
        _db.RideRequests.Include(r => r.Snapshots)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task AddAsync(RideRequest request, CancellationToken ct) =>
        await _db.RideRequests.AddAsync(request, ct);

    public async Task<IReadOnlyList<RideRequest>> GetByUserIdAsync(string userId, int limit, CancellationToken ct) =>
        await _db.RideRequests
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}

public class PriceSnapshotRepository : IPriceSnapshotRepository
{
    private readonly PricingDbContext _db;
    public PriceSnapshotRepository(PricingDbContext db) => _db = db;

    public async Task AddRangeAsync(IEnumerable<PriceSnapshot> snapshots, CancellationToken ct)
    {
        await _db.PriceSnapshots.AddRangeAsync(snapshots, ct);
    }

    public async Task<IReadOnlyList<PriceSnapshot>> GetHistoryAsync(
        string origin, string destination,
        string? providerId, DateTime from, DateTime to, CancellationToken ct)
    {
        var query = _db.PriceSnapshots
            .Include(s => s.Provider)
            .Include(s => s.RideRequest)
            .Where(s => s.RideRequest.Origin == origin
                     && s.RideRequest.Destination == destination
                     && s.CapturedAt >= from
                     && s.CapturedAt <= to);

        if (!string.IsNullOrEmpty(providerId) && Guid.TryParse(providerId, out var pid))
            query = query.Where(s => s.ProviderId == pid);

        return await query.OrderBy(s => s.CapturedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<PriceSnapshot>> GetLatestForRequestAsync(Guid requestId, CancellationToken ct) =>
        await _db.PriceSnapshots
            .Include(s => s.Provider)
            .Where(s => s.RideRequestId == requestId)
            .OrderByDescending(s => s.CapturedAt)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
