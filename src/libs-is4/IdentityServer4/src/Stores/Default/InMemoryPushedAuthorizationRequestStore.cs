using System;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Threading.Tasks;

namespace IdentityServer4.Stores;

internal class InMemoryPushedAuthorizationRequestStore : IPushedAuthorizationRequestStore
{
    private readonly ConcurrentDictionary<string, (NameValueCollection Parameters, DateTimeOffset ExpiresAt)> _store = new();

    public Task StoreAsync(string requestUri, NameValueCollection parameters, int expiresInSeconds = 60)
    {
        var expiry = DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds);
        _store[requestUri] = (parameters, expiry);
        return Task.CompletedTask;
    }

    public Task<NameValueCollection> GetAsync(string requestUri)
    {
        if (_store.TryGetValue(requestUri, out var entry))
        {
            if (entry.ExpiresAt > DateTimeOffset.UtcNow)
                return Task.FromResult(entry.Parameters);

            _store.TryRemove(requestUri, out _);
        }
        return Task.FromResult<NameValueCollection>(null);
    }

    public Task RemoveAsync(string requestUri)
    {
        _store.TryRemove(requestUri, out _);
        return Task.CompletedTask;
    }
}
