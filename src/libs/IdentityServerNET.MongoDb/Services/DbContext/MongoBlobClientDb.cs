using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.MongoDb.MongoDocuments;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.MongoDb.Services.DbContext;

public class MongoBlobClientDb : IClientDbContextModify
{
    protected string _connectionString = null;
    private ICryptoService _cryptoService = null;
    private IBlobSerializer _blobSerializer = null;

    private string _databaseName = "identityserver";
    private string _collectionName = "clients";

    public MongoBlobClientDb(
            IOptions<ClientDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer blobSerializer = null
        )
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("MongoBlobClientDb: no connection string defined");
        }

        _connectionString = options.Value.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    #region  IClientDbContext

    async public Task<ClientModel> FindClientByIdAsync(string clientId)
    {
        string id = clientId.ClientIdToHexId(_cryptoService);

        var collection = GetCollection();

        var document = await (await collection.FindAsync<ClientBlobDocument>(
            filter: Builders<ClientBlobDocument>.Filter.Eq("_id", id))
            ).FirstOrDefaultAsync();

        if (document != null)
        {
            return _blobSerializer.DeserializeObject<ClientModel>(
                _cryptoService.DecryptText(document.BlobData));
        }

        return null;
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

        string id = client.ClientId.ClientIdToHexId(_cryptoService);

        var collection = GetCollection();

        var document = new ClientBlobDocument()
        {
            Id = id,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(client))
        };

        await collection.InsertOneAsync(document);
    }

    async public Task<IEnumerable<ClientModel>> GetAllClients()
    {
        var collection = GetCollection();

        IMongoQueryable<ClientBlobDocument> query = collection
                .AsQueryable<ClientBlobDocument>()
                .Where(_ => true);

        var result = new List<ClientModel>();

        var cursor = await query.ToCursorAsync();
        while (await cursor.MoveNextAsync())
        {
            foreach (var clientDocument in cursor.Current)
            {
                if (clientDocument.Id.IsValidClientHexId())
                {
                    result.Add(_blobSerializer.DeserializeObject<ClientModel>(_cryptoService.DecryptText(clientDocument.BlobData)));
                }
            }
        }

        return result;
    }

    async public Task RemoveClientAsync(ClientModel client)
    {
        var collection = GetCollection();

        string id = client.ClientId.ClientIdToHexId(_cryptoService);

        await collection
            .DeleteOneAsync<ClientBlobDocument>(d => d.Id == id);
    }

    async public Task UpdateClientAsync(ClientModel client, IEnumerable<string> propertyNames = null)
    {
        var collection = GetCollection();

        string id = client.ClientId.ClientIdToHexId(_cryptoService);

        UpdateDefinition<ClientBlobDocument> update = Builders<ClientBlobDocument>
            .Update
            .Set("BlobData", _cryptoService.EncryptText(_blobSerializer.SerializeObject(client)));

        await collection
            .UpdateOneAsync<ClientBlobDocument>(d => d.Id == id, update);
    }

    #endregion

    #region Helper

    private IMongoCollection<ClientBlobDocument> GetCollection()
    {
        var client = new MongoClient(_connectionString);
        var database = client.GetDatabase(_databaseName);
        var collection = database.GetCollection<ClientBlobDocument>(_collectionName);

        return collection;
    }

    #endregion
}
