using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Azure.Services.DbContext;

public class TableStorageBlobRealmDb : IRealmDbContext
{
    protected string _connectionString = null;
    private ICryptoService _cryptoService = null;
    private IBlobSerializer _blobSerializer = null;

    private string _tablename = "IdentityServer";
    internal static readonly string PartitionKey = "identityserver-realms";
    private AzureTableStorage<BlobTableEntity> _tableStorage;

    public TableStorageBlobRealmDb(
            IOptions<RealmDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer blobSerializer = null)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("TableStorageBlobRealmDb: no connection string defined");
        }

        _connectionString = options.Value.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();

        _tableStorage = new AzureTableStorage<BlobTableEntity>();
        _tableStorage.Init(_connectionString);
    }

    async public Task<RealmModel> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();
        var entity = await _tableStorage.EntityAsync(_tablename, PartitionKey, realmName);
        return entity.Deserialize<RealmModel>(_cryptoService, _blobSerializer);
    }

    async public Task<RealmModel> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    async public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        return (await _tableStorage.AllEntitiesAsync(_tablename, PartitionKey))
            .Select(e => e.Deserialize<RealmModel>(_cryptoService, _blobSerializer))
            .Where(r => r != null)
            .ToArray();
    }

    async public Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (await FindByNameAsync(realm.Name, cancellationToken) != null)
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);
        realm.CreateDate = realm.CreateDate == default ? DateTime.UtcNow : realm.CreateDate;

        await _tableStorage.CreateTableAsync(_tablename);
        await _tableStorage.InsertEntityAsync(_tablename,
            new BlobTableEntity(PartitionKey, realm.Name, realm, _cryptoService, _blobSerializer));
    }

    async public Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();
        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        await _tableStorage.MergeEntityAsync(_tablename,
            new BlobTableEntity(PartitionKey, realm.Name, realm, _cryptoService, _blobSerializer));
    }

    async public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var name = (realm.Name ?? "").Trim().ToLowerInvariant();
        await _tableStorage.DeleteEntityAsync(_tablename, PartitionKey, name);
    }
}
