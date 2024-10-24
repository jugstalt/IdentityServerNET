using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.LiteDb.Documents;
using IdentityServerNET.LiteDb.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.Serialize;
using LiteDB;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.LiteDb.Services.DbContext;
public class LiteDbResourceDb : IResourceDbContextModify
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    //private const string DatabaseName = "identityserver";
    private const string ApiCollectionName = "apiresources";
    private const string IdentityCollectionName = "identityresources";

    public LiteDbResourceDb(
            IOptions<ResourceDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer? blobSerializer = null
        )
    {
        _connectionString = options.Value.ConnectionString.EnsureLiteDbParentDirectoryCreated();
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    #region IResourceDbContext

    public Task<ApiResourceModel?> FindApiResourceAsync(string name)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ApiCollectionName);

            var blob = collection.Query()
                                 .Where(x => x.Name == name)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<ApiResourceModel?>(
                        _blobSerializer.DeserializeObject<ApiResourceModel>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ApiResourceModel?>(null);
        }
    }

    async public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
    {
        List<ApiResourceModel> apiResources = new List<ApiResourceModel>();

        foreach (var scopeName in scopeNames)
        {
            var apiResource = await FindApiResourceAsync(scopeName);
            if (apiResource != null)
            {
                apiResources.Add(apiResource);
            }
        }

        return apiResources;
    }

    public Task<IdentityResourceModel?> FindIdentityResource(string name)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(IdentityCollectionName);

            var blob = collection.Query()
                                 .Where(x => x.Name == name)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<IdentityResourceModel?>(
                        _blobSerializer.DeserializeObject<IdentityResourceModel>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<IdentityResourceModel?>(null);
        }
    }

    public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ApiCollectionName);

            var blobs = collection.FindAll();

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ApiResourceModel>>(
                    blobs.Select(blob =>
                            _blobSerializer.DeserializeObject<ApiResourceModel>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ApiResourceModel>>(Array.Empty<ApiResourceModel>());
        }
    }

    public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(IdentityCollectionName);

            var blobs = collection.FindAll();

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<IdentityResourceModel>>(
                    blobs.Select(blob =>
                            _blobSerializer.DeserializeObject<IdentityResourceModel>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<IdentityResourceModel>>(Array.Empty<IdentityResourceModel>());
        }
    }

    #endregion

    #region IResourceDbContextModify

    async public Task AddApiResourceAsync(ApiResourceModel apiResource)
    {
        if (apiResource == null)
        {
            return;
        }

        if (await FindApiResourceAsync(apiResource.Name) != null)
        {
            throw new Exception("Resource alread exists");
        }

        var blob = new LiteDbBlobDocument()
        {
            Name = apiResource.Name,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(apiResource))
        };

        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ApiCollectionName);

            collection.Insert(blob);
        }
    }

    public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ApiCollectionName);

            collection.DeleteMany(b => b.Name == apiResource.Name);

            return Task.CompletedTask;
        }
    }

    public Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string>? propertyNames = null)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ApiCollectionName);

            var blob = collection.FindOne(b => b.Name == apiResource.Name);
            if (blob == null)
            {
                throw new Exception($"Resource with name = {apiResource.Name} not exists");
            }

            blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(apiResource));

            collection.Update(blob);

            return Task.CompletedTask;
        }
    }

    async public Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        if (identityResource == null)
        {
            return;
        }

        if (await FindApiResourceAsync(identityResource.Name) != null)
        {
            throw new Exception("Identity-resource alread exists");
        }

        var blob = new LiteDbBlobDocument()
        {
            Name = identityResource.Name,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(identityResource))
        };

        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(IdentityCollectionName);

            collection.Insert(blob);
        }
    }

    public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(IdentityCollectionName);

            collection.DeleteMany(b => b.Name == identityResource.Name);

            return Task.CompletedTask;
        }
    }

    public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string>? propertyNames = null)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(IdentityCollectionName);

            var blob = collection.FindOne(b => b.Name == identityResource.Name);
            if (blob == null)
            {
                throw new Exception($"Identity-resource with name = {identityResource.Name} not exists");
            }

            blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(identityResource));

            collection.Update(blob);

            return Task.CompletedTask;
        }
    }

    #endregion
}
