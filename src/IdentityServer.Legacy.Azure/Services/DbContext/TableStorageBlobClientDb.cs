using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
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
        private AzureTableStorage _tableStorage;

        public TableStorageBlobClientDb(IOptions<ClientDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("MongoBlobClientDb: no connection string defined");

            _connectionString = options.Value.ConnectionString;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            _tableStorage = new AzureTableStorage();
            _tableStorage.Init(_connectionString);
        }

        #region IClientDbContext

        async public Task<Client> FindClientByIdAsync(string clientId)
        {
            if (_tablename == null || String.IsNullOrWhiteSpace(clientId))
                return null;

            await _tableStorage.CreateTableAsync(_tablename);
            var tableEntity = await _tableStorage.EntityAsync(_tablename, PartitionKey, clientId);

            return ClientTableEntity.ToClient(tableEntity, _cryptoService, _blobSerializer);
        }

        #endregion

        #region IClientDbContextModify

        async public Task AddClientAsync(Client client)
        {
            if (_tableStorage == null || client == null)
                return;

            await _tableStorage.CreateTableAsync(_tablename);
            await _tableStorage.InsertEntityAsync(_tablename,
                new ClientTableEntity(client, _cryptoService, _blobSerializer));
        }

        public Task UpdateClientAsync(Client client, IEnumerable<string> propertyNames = null)
        {
            throw new NotImplementedException();
        }

        async public Task RemoveClientAsync(Client client)
        {
            if (_tableStorage == null || client == null)
                return;

            await _tableStorage.CreateTableAsync(_tablename);
            await _tableStorage.DeleteEntityAsync(_tablename,
                new ClientTableEntity(client, _cryptoService, _blobSerializer));
        }

        async public Task<IEnumerable<Client>> GetAllClients()
        {
            if (_tableStorage == null)
                return new Client[0];

            return (await _tableStorage.AllEntitiesAsync(_tablename, PartitionKey))
                       .Select(e => ClientTableEntity.ToClient(e, _cryptoService, _blobSerializer))
                       .ToArray();
        }

        #endregion

        #region Classes

        public class ClientTableEntity : TableEntity
        {
            public ClientTableEntity() { }

            public ClientTableEntity(Client client, ICryptoService cryptoService, IBlobSerializer blobSerializer)
            {
                this.PartitionKey = TableStorageBlobClientDb.PartitionKey;
                this.RowKey = client.ClientId;

                var data = cryptoService.EncryptText(blobSerializer.SerializeObject(client));
                _properties.Add("Blob", new EntityProperty(data));
            }

            private IDictionary<string, EntityProperty> _properties = new Dictionary<string, EntityProperty>();

            #region Static Membmers

            public static Client ToClient(TableEntity entity, ICryptoService cryptoService, IBlobSerializer blobSerializer)
            {
                var properties = entity?.WriteEntity(new OperationContext());
                if (properties == null)
                {
                    return null;
                }

                var blobBase64 = properties["Blob"]?.StringValue;

                return blobSerializer.DeserializeObject<Client>(cryptoService.DecryptText(blobBase64));
            }

            #endregion

            #region Overrides

            public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
            {
                _properties = properties;
            }

            public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
            {
                return _properties ?? new Dictionary<string, EntityProperty>();
            }

            #endregion
        }

        #endregion
    }
}
