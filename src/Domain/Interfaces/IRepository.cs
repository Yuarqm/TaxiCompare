using TaxiCompare.Domain.Entities;

namespace TaxiCompare.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
}

public interface IRideRequestRepository : IRepository<RideRequest>
{
    Task<IEnumerable<RideRequest>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<RideRequest>> GetPopularRoutesAsync(int count = 10, CancellationToken cancellationToken = default);
}

public interface IPriceSnapshotRepository : IRepository<PriceSnapshot>
{
    Task<IEnumerable<PriceSnapshot>> GetByRideRequestAsync(Guid rideRequestId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceSnapshot>> GetHistoryAsync(Guid providerId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IEnumerable<PriceSnapshot>> GetLatestForRouteAsync(double originLat, double originLng, double destLat, double destLng, CancellationToken cancellationToken = default);
}

public interface IProviderRepository : IRepository<Provider>
{
    Task<Provider?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Provider>> GetActiveAsync(CancellationToken cancellationToken = default);
}
