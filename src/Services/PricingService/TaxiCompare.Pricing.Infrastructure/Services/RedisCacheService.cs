using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TaxiCompare.Pricing.Application.Interfaces;

namespace TaxiCompare.Pricing.Infrastructure.Services;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var data = await _cache.GetStringAsync(key, ct);
        if (data is null) return default;
        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken ct = default)
    {
        var data = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, data, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry
        }, ct);
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        _cache.RemoveAsync(key, ct);
}
