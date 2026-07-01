using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class InMemoryRealmDb : IRealmDbContext
{
    private static readonly ConcurrentDictionary<string, RealmModel> _realms =
        new ConcurrentDictionary<string, RealmModel>();

    public InMemoryRealmDb(IOptions<RealmDbContextConfiguration> options = null)
    {
    }

    public Task<RealmModel> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();

        return Task.FromResult(_realms.TryGetValue(realmName, out var realm) ? realm : null);
    }

    public Task<RealmModel> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();

        var realm = _realms.Values.FirstOrDefault(
            r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));

        return Task.FromResult(realm);
    }

    public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<RealmModel>>(_realms.Values.ToArray());

    public async Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (_realms.ContainsKey(realm.Name))
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        realm.CreateDate = realm.CreateDate == default ? DateTime.UtcNow : realm.CreateDate;

        if (!_realms.TryAdd(realm.Name, realm))
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        }
    }

    public async Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (!_realms.ContainsKey(realm.Name))
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' does not exist.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        _realms[realm.Name] = realm;
    }

    public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        _realms.TryRemove((realm.Name ?? "").Trim().ToLowerInvariant(), out _);

        return Task.CompletedTask;
    }
}
