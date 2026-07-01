using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.LiteDb.Documents;
using IdentityServerNET.LiteDb.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Serialize;
using LiteDB;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.LiteDb.Services.DbContext;

public class LiteDbRealmDb : IRealmDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string RealmsCollectionName = "realms";

    public LiteDbRealmDb(
            IOptions<RealmDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer? blobSerializer = null
        )
    {
        _connectionString = options.Value.ConnectionString.EnsureLiteDbParentDirectoryCreated();
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetBlobDocumentCollection(RealmsCollectionName);
        var blob = collection.Query().Where(x => x.Name == realmName).FirstOrDefault();

        return Task.FromResult(blob is null ? null : Deserialize(blob));
    }

    public async Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();

        var all = await GetAllAsync(cancellationToken);

        return all.FirstOrDefault(
            r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetBlobDocumentCollection(RealmsCollectionName);
        var blobs = collection.FindAll();

        return Task.FromResult<IEnumerable<RealmModel>>(
            blobs is null ? Array.Empty<RealmModel>() : blobs.Select(Deserialize).ToArray());
    }

    public async Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (await FindByNameAsync(realm.Name, cancellationToken) is not null)
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        realm.CreateDate = realm.CreateDate == default ? DateTime.UtcNow : realm.CreateDate;

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetBlobDocumentCollection(RealmsCollectionName);
        collection.Insert(new LiteDbBlobDocument
        {
            Name = realm.Name,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm))
        });
    }

    public async Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetBlobDocumentCollection(RealmsCollectionName);
        var blob = collection.FindOne(b => b.Name == realm.Name);
        if (blob is null)
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' does not exist.");
        }

        blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm));
        collection.Update(blob);
    }

    public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var name = (realm.Name ?? "").Trim().ToLowerInvariant();

        using var db = new LiteDatabase(_connectionString);
        var collection = db.GetBlobDocumentCollection(RealmsCollectionName);
        collection.DeleteMany(b => b.Name == name);

        return Task.CompletedTask;
    }

    private RealmModel Deserialize(LiteDbBlobDocument blob)
        => _blobSerializer.DeserializeObject<RealmModel>(_cryptoService.DecryptText(blob.BlobData));
}
