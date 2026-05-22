using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using TaxiCompare.Application.Interfaces;

namespace TaxiCompare.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private static readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    public RedisCacheService(IDistributedCache cache) => _cache = cache;

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var bytes = await _cache.GetAsync(key, ct);
        if (bytes is null) return default;
        return JsonSerializer.Deserialize<T>(bytes, _options);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, _options);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiry ?? TimeSpan.FromMinutes(5)
        };
        await _cache.SetAsync(key, bytes, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default) =>
        await _cache.RemoveAsync(key, ct);

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        await _cache.GetAsync(key, ct) is not null;
}
