using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.MongoDb.MongoDocuments;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.MongoDb.Services.DbContext
{
    public class MongoBlobClientDb : IClientDbContextModify
    {
        protected string _connectionString = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer = null;

        private string _databaseName = "identityserver";
        private string _collectionName = "clients";

        public MongoBlobClientDb(IOptions<ClientDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("MongoBlobClientDb: no connection string defined");

            _connectionString = options.Value.ConnectionString;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            //DirectoryInfo di = new DirectoryInfo(_connectionString);
            //if (!di.Exists)
            //{
            //    di.Create();

            //    // Initialize Api Clients
            //    if (options.Value.IntialClients != null)
            //    {
            //        foreach (var client in options.Value.IntialClients)
            //        {
            //            AddClientAsync(client).Wait();
            //        }
            //    }
            //}
        }

        #region  IClientDbContext

        async public Task<Client> FindClientByIdAsync(string clientId)
        {
            string id = clientId.NameToHexId(_cryptoService);

            var collection = GetCollection();

            var document = await(await collection.FindAsync<ClientBlobDocument>(
                filter: Builders<ClientBlobDocument>.Filter.Eq("_id", id))
                ).FirstOrDefaultAsync();

            if (document != null)
            {
                return _blobSerializer.DeserializeObject<Client>(
                    _cryptoService.DecryptText(document.BlobData));
            }

            return null;
        }

        #endregion

        #region IClientDbContextModify

        async public Task AddClientAsync(Client client)
        {
            if (client == null)
            {
                return;
            }

            string id = client.ClientId.NameToHexId(_cryptoService);
            // ToDo: Check if id exists;

            var collection = GetCollection();

            var document = new ClientBlobDocument()
            {
                Id = id,
                BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(client))
            };

            await collection.InsertOneAsync(document);
        }

        async public Task<IEnumerable<Client>> GetAllClients()
        {
            var collection = GetCollection();

            IMongoQueryable<ClientBlobDocument> query = collection
                    .AsQueryable<ClientBlobDocument>()
                    .Where(_ => true);

            var result = new List<Client>();

            var cursor = await query.ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var clientDocument in cursor.Current)
                {
                    result.Add(_blobSerializer.DeserializeObject<Client>(_cryptoService.DecryptText(clientDocument.BlobData)));
                }
            }

            return result;
        }

        async public Task RemoveClientAsync(Client client)
        {
            var collection = GetCollection();

            string id = client.ClientId.NameToHexId(_cryptoService);

            await collection
                .DeleteOneAsync<ClientBlobDocument>(d => d.Id == id);
        }

        async public Task UpdateClientAsync(Client client, IEnumerable<string> propertyNames = null)
        {
            var collection = GetCollection();

            string id = client.ClientId.NameToHexId(_cryptoService);

            UpdateDefinition<ClientBlobDocument> update =  Builders<ClientBlobDocument>
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
}
