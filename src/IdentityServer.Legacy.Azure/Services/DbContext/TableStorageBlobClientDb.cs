using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    public class TableStorageBlobClientDb : IClientDbContextModify
    {
        protected string _connectionString = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer = null;

        private string _tablename = "IdentityServer";
        internal static readonly  string PartitionKey = "identityserver-clients";
        private AzureTableStorage<BlobTableEntity> _tableStorage;

        public TableStorageBlobClientDb(IOptions<ClientDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("TableStorageBlobClientDb: no connection string defined");

            _connectionString = options.Value.ConnectionString;
            _tablename = !String.IsNullOrWhiteSpace(options.Value.TableName) ?
                                    options.Value.TableName :
                                    _tablename;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            _tableStorage = new AzureTableStorage<BlobTableEntity>();
            _tableStorage.Init(_connectionString);
        }

        #region IClientDbContext

        async public Task<ClientModel> FindClientByIdAsync(string clientId)
        {
            if (_tablename == null || String.IsNullOrWhiteSpace(clientId))
                return null;

            var tableEntity = await _tableStorage.EntityAsync(_tablename, PartitionKey, clientId);

            return tableEntity.Deserialize<ClientModel>(_cryptoService, _blobSerializer);
        }

        #endregion

        #region IClientDbContextModify

        async public Task AddClientAsync(ClientModel client)
        {
            if (_tableStorage == null || client == null)
                return;

            await _tableStorage.CreateTableAsync(_tablename);
            await _tableStorage.InsertEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobClientDb.PartitionKey,
                                    client.RowKey(),
                                    client,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task UpdateClientAsync(ClientModel client, IEnumerable<string> propertyNames = null)
        {
            if (_tableStorage == null || client == null)
                return;

            await _tableStorage.MergeEntity(_tablename,
                new BlobTableEntity(TableStorageBlobClientDb.PartitionKey,
                                    client.RowKey(),
                                    client,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task RemoveClientAsync(ClientModel client)
        {
            if (_tableStorage == null || client == null)
                return;

            await _tableStorage.DeleteEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobClientDb.PartitionKey,
                                    client.RowKey(),
                                    client,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task<IEnumerable<ClientModel>> GetAllClients()
        {
            if (_tableStorage == null)
                return new ClientModel[0];

            return (await _tableStorage.AllEntitiesAsync(_tablename, PartitionKey))
                       .Select(e => e.Deserialize<ClientModel>(_cryptoService, _blobSerializer))
                       .ToArray();
        }

        #endregion
    }
}
