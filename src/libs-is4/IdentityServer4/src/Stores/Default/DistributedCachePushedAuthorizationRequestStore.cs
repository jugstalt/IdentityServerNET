using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Specialized;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace IdentityServer4.Stores.Default;

public class DistributedCachePushedAuthorizationRequestStore : IPushedAuthorizationRequestStore
{
    private const string CacheKeyPrefix = "PAR_";
    private readonly IDistributedCache _cache;

    public DistributedCachePushedAuthorizationRequestStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public Task StoreAsync(string requestUri, NameValueCollection parameters, int expiresInSeconds = 60)
    {
        var dict = parameters.AllKeys
            .Where(k => k != null)
            .ToDictionary(k => k!, k => parameters[k] ?? "");

        var json = JsonSerializer.Serialize(dict);
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = System.TimeSpan.FromSeconds(expiresInSeconds)
        };

        return _cache.SetStringAsync(CacheKeyPrefix + requestUri, json, options);
    }

    public async Task<NameValueCollection> GetAsync(string requestUri)
    {
        var json = await _cache.GetStringAsync(CacheKeyPrefix + requestUri);
        if (json == null) return null;

        var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
        var nvc = new NameValueCollection();
        foreach (var kv in dict!) nvc[kv.Key] = kv.Value;
        return nvc;
    }

    public Task RemoveAsync(string requestUri)
        => _cache.RemoveAsync(CacheKeyPrefix + requestUri);
}
