using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Serialize;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    public class TableStorageBlobResourceDb : IResourceDbContextModify
    {
        protected string _connectionString = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer = null;

        private string _tablename = "IdentityServer";
        internal static readonly string ApiResourcePartitionKey = "identityserver-api-resources";
        internal static readonly string IdentityResourcePartitionKey = "identityserver-identity-resources";
        private AzureTableStorage<BlobTableEntity> _tableStorage;

        public TableStorageBlobResourceDb(IOptions<ResourceDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
            {
                throw new ArgumentException("TableStorageBlobResourceDb: no connection string defined");
            }

            _connectionString = options.Value.ConnectionString;
            _tablename = !String.IsNullOrWhiteSpace(options.Value.TableName) ?
                                    options.Value.TableName :
                                    _tablename;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            _tableStorage = new AzureTableStorage<BlobTableEntity>();
            _tableStorage.Init(_connectionString);
        }

        #region IResourceDbContextModify

        async public Task AddApiResourceAsync(ApiResourceModel apiResource)
        {
            if (_tableStorage == null || apiResource == null)
            {
                return;
            }

            await _tableStorage.CreateTableAsync(_tablename);
            await _tableStorage.InsertEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.ApiResourcePartitionKey,
                                    apiResource.RowKey(),
                                    apiResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string> propertyNames = null)
        {
            if (_tableStorage == null || apiResource == null)
            {
                return;
            }

            await _tableStorage.MergeEntity(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.ApiResourcePartitionKey,
                                    apiResource.RowKey(),
                                    apiResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
        {
            if (_tableStorage == null || apiResource == null)
            {
                return;
            }

            await _tableStorage.DeleteEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.ApiResourcePartitionKey,
                                    apiResource.RowKey(),
                                    apiResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
        {
            if (_tableStorage == null || identityResource == null)
            {
                return;
            }

            await _tableStorage.CreateTableAsync(_tablename);
            await _tableStorage.InsertEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.IdentityResourcePartitionKey,
                                    identityResource.RowKey(),
                                    identityResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string> propertyNames = null)
        {
            if (_tableStorage == null || identityResource == null)
            {
                return;
            }

            await _tableStorage.MergeEntity(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.IdentityResourcePartitionKey,
                                    identityResource.RowKey(),
                                    identityResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
        {
            if (_tableStorage == null || identityResource == null)
            {
                return;
            }

            await _tableStorage.DeleteEntityAsync(_tablename,
                new BlobTableEntity(TableStorageBlobResourceDb.IdentityResourcePartitionKey,
                                    identityResource.RowKey(),
                                    identityResource,
                                    _cryptoService,
                                    _blobSerializer));
        }

        async public Task<ApiResourceModel> FindApiResourceAsync(string name)
        {
            if (_tablename == null || String.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var tableEntity = await _tableStorage.EntityAsync(_tablename, ApiResourcePartitionKey, name);

            return tableEntity.Deserialize<ApiResourceModel>(_cryptoService, _blobSerializer);
        }

        async public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            var apis = (await GetAllApiResources())
                        .Where(a => scopeNames.Contains(a.Name));
            return apis;
        }

        async public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
        {
            if (_tableStorage == null)
            {
                return new ApiResourceModel[0];
            }

            return (await _tableStorage.AllEntitiesAsync(_tablename, ApiResourcePartitionKey))
                       .Select(e => e.Deserialize<ApiResourceModel>(_cryptoService, _blobSerializer))
                       .ToArray();
        }

        async public Task<IdentityResourceModel> FindIdentityResource(string name)
        {
            if (_tablename == null || String.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var tableEntity = await _tableStorage.EntityAsync(_tablename, IdentityResourcePartitionKey, name);

            return tableEntity.Deserialize<IdentityResourceModel>(_cryptoService, _blobSerializer);
        }

        async public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
        {
            if (_tableStorage == null)
            {
                return new IdentityResourceModel[0];
            }

            return (await _tableStorage.AllEntitiesAsync(_tablename, IdentityResourcePartitionKey))
                       .Select(e => e.Deserialize<IdentityResourceModel>(_cryptoService, _blobSerializer))
                       .ToArray();
        }

        #endregion
    }
}
