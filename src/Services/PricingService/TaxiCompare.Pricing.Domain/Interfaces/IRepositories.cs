using TaxiCompare.Pricing.Domain.Entities;

namespace TaxiCompare.Pricing.Domain.Interfaces;

public interface IProviderRepository
{
    Task<IReadOnlyList<Provider>> GetActiveProvidersAsync(CancellationToken ct = default);
    Task<Provider?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Provider provider, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IRideRequestRepository
{
    Task<RideRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(RideRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<RideRequest>> GetByUserIdAsync(string userId, int limit = 10, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IPriceSnapshotRepository
{
    Task AddRangeAsync(IEnumerable<PriceSnapshot> snapshots, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetHistoryAsync(string origin, string destination,
        string? providerId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<PriceSnapshot>> GetLatestForRequestAsync(Guid requestId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

public interface IUserPreferenceRepository
{
    Task<UserPreference?> GetByUserIdAsync(string userId, CancellationToken ct = default);
    Task AddAsync(UserPreference preference, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
