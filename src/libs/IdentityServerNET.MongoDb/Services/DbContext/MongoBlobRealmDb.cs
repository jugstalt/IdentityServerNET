using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.MongoDb.MongoDocuments;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.MongoDb.Services.DbContext;

public class MongoBlobRealmDb : IRealmDbContext
{
    private string _connectionString = null;
    private ICryptoService _cryptoService = null;
    private IBlobSerializer _blobSerializer = null;

    private string _databaseName = "identityserver";
    private string _collectionName = "realms";

    public MongoBlobRealmDb(
            IOptions<RealmDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer blobSerializer = null)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("MongoBlobRealmDb: no connection string defined");
        }

        _connectionString = options.Value.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    async public Task<RealmModel> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();

        var collection = GetCollection();
        var document = await (await collection.FindAsync<RealmBlobDocument>(
            Builders<RealmBlobDocument>.Filter.Eq("_id", realmName))).FirstOrDefaultAsync();

        return document != null ? Deserialize(document) : null;
    }

    async public Task<RealmModel> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();

        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    async public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        var collection = GetCollection();

        IMongoQueryable<RealmBlobDocument> query = collection
            .AsQueryable<RealmBlobDocument>()
            .Where(_ => true);

        var result = new List<RealmModel>();

        var cursor = await query.ToCursorAsync();
        while (await cursor.MoveNextAsync())
        {
            foreach (var document in cursor.Current)
            {
                result.Add(Deserialize(document));
            }
        }

        return result;
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

        var collection = GetCollection();
        await collection.InsertOneAsync(new RealmBlobDocument
        {
            Id = realm.Name,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm))
        });
    }

    async public Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();
        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        var collection = GetCollection();
        var update = Builders<RealmBlobDocument>.Update.Set("BlobData",
            _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm)));

        await collection.UpdateOneAsync<RealmBlobDocument>(d => d.Id == realm.Name, update);
    }

    async public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var name = (realm.Name ?? "").Trim().ToLowerInvariant();
        var collection = GetCollection();
        await collection.DeleteOneAsync<RealmBlobDocument>(d => d.Id == name);
    }

    private RealmModel Deserialize(RealmBlobDocument document)
        => _blobSerializer.DeserializeObject<RealmModel>(_cryptoService.DecryptText(document.BlobData));

    private IMongoCollection<RealmBlobDocument> GetCollection()
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        return database.GetCollection<RealmBlobDocument>(_collectionName);
    }
}
