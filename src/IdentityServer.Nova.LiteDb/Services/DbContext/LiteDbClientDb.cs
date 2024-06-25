using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.LiteDb.Documents;
using IdentityServer.Nova.LiteDb.Extensions;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.Services.Serialize;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Xml.Linq;

namespace IdentityServer.Nova.LiteDb.Services.DbContext;
public class LiteDbClientDb : IClientDbContextModify
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string ClientsCollectionName = "clients";

    public LiteDbClientDb(IOptions<ClientDbContextConfiguration> options)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("LiteDbClientDb: no connection string defined");
        }

        _connectionString = options.Value.ConnectionString;
        _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
        _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();
    }

    #region IClientDbContext

    public Task<ClientModel?> FindClientByIdAsync(string clientId)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ClientsCollectionName);
            var blob = collection.Query()
                                 .Where(x => x.Name == clientId)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<ClientModel?>(
                        _blobSerializer.DeserializeObject<ClientModel>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ClientModel?>(null);
        }
    }

    #endregion

    #region IClientDbContextModify

    async public Task AddClientAsync(ClientModel client)
    {
        if (client == null)
        {
            return;
        }

        if (await FindClientByIdAsync(client.ClientId) != null)
        {
            throw new Exception("client alread exists");
        }

        var blob = new LiteDbBlobDocument()
        {
            Name = client.ClientId,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(client))
        };

        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ClientsCollectionName);

            collection.Insert(blob);
        }
    }

    public Task<IEnumerable<ClientModel>> GetAllClients()
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ClientsCollectionName);

            var blobs = collection.FindAll();

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ClientModel>>(
                    blobs.Select(blob =>
                            _blobSerializer.DeserializeObject<ClientModel>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ClientModel>>(Array.Empty<ClientModel>());
        }
    }

    public Task RemoveClientAsync(ClientModel client)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ClientsCollectionName);

            collection.DeleteMany(b => b.Name == client.ClientId);

            return Task.CompletedTask;
        }
    }

    public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(ClientsCollectionName);

            var blob = collection.FindOne(b => b.Name == client.ClientId);
            if(blob == null)
            {
                throw new Exception($"client with clientId = {client.ClientId} not exists");
            }

            blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(client));

            collection.Update(blob);

            return Task.CompletedTask;
        }
    }

    #endregion
}
