using IdentityServer.Legacy.Extensions.DependencyInjection;
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
    public class MongoBlobResourceDb : IResourceDbContextModify
    {
        protected string _connectionString = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer = null;

        private string _databaseName = "identityserver";
        private string _apiCollectionName = "apiresources";
        private string _identityCollectionName = "identityresources";

        public MongoBlobResourceDb(IOptions<ResourceDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("FileBlobResourceDb: no connection string defined");

            _connectionString = options.Value.ConnectionString;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            //DirectoryInfo di = new DirectoryInfo(_connectionString);
            //if (!di.Exists)
            //{
            //    di.Create();

            //    // Initialize Api Resources
            //    if (options.Value.InitialApiResources != null)
            //    {
            //        foreach (var apiResource in options.Value.InitialApiResources)
            //        {
            //            AddApiResourceAsync(apiResource).Wait();
            //        }
            //    }
            //    // Initialize Identity Resources
            //    if (options.Value.InitialIdentityResources != null)
            //    {

            //    }
            //}
        }

        

        #region IResourceDbContext

        async public Task<ApiResource> FindApiResourceAsync(string name)
        {
            string id = name.ApiNameToHexId(_cryptoService);

            var collection = GetApiResourceCollection();

            var document = await(await collection.FindAsync<ApiResourceDocument>(
                filter: Builders<ApiResourceDocument>.Filter.Eq("_id", id))
                ).FirstOrDefaultAsync();

            if (document != null)
            {
                return _blobSerializer.DeserializeObject<ApiResource>(
                    _cryptoService.DecryptText(document.BlobData));
            }

            return null;
        }

        async public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            List<ApiResource> apiResources = new List<ApiResource>();

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

        async public Task<IdentityResource> FindIdentityResource(string name)
        {
            string id = name.IdentityNameToHexId(_cryptoService);

            var collection = GetIdentityResourceCollection();

            var document = await(await collection.FindAsync<IdentityResourceDocument>(
                filter: Builders<IdentityResourceDocument>.Filter.Eq("_id", id))
                ).FirstOrDefaultAsync();

            if (document != null)
            {
                return _blobSerializer.DeserializeObject<IdentityResource>(
                    _cryptoService.DecryptText(document.BlobData));
            }


            return null;
        }

        async public Task<IEnumerable<ApiResource>> GetAllApiResources()
        {
            var collection = GetApiResourceCollection();

            IMongoQueryable<ApiResourceDocument> query = collection
                    .AsQueryable<ApiResourceDocument>()
                    .Where(_ => true);

            var result = new List<ApiResource>();

            var cursor = await query.ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var apiResourceDocument in cursor.Current)
                {
                    if (apiResourceDocument.Id.IsValidApiResourceHexId())
                    {
                        result.Add(_blobSerializer.DeserializeObject<ApiResource>(_cryptoService.DecryptText(apiResourceDocument.BlobData)));
                    }
                }
            }

            return result;
        }

        async public Task<IEnumerable<IdentityResource>> GetAllIdentityResources()
        {
            var collection = GetIdentityResourceCollection();

            IMongoQueryable<IdentityResourceDocument> query = collection
                    .AsQueryable<IdentityResourceDocument>()
                    .Where(_ => true);

            var result = new List<IdentityResource>();

            var cursor = await query.ToCursorAsync();
            while (await cursor.MoveNextAsync())
            {
                foreach (var identityResourceDocument in cursor.Current)
                {
                    if (identityResourceDocument.Id.IsValidIdentityResourceHexId())
                    {
                        result.Add(_blobSerializer.DeserializeObject<IdentityResource>(_cryptoService.DecryptText(identityResourceDocument.BlobData)));
                    }
                }
            }

            return result;
        }

        #endregion

        #region  IResourceDbContextModify

        async public Task AddApiResourceAsync(ApiResource apiResource)
        {
            if (apiResource == null)
            {
                return;
            }

            if(await FindApiResourceAsync(apiResource.Name)!=null)
            {
                throw new Exception("Api resource alread exists");
            }

            string id = apiResource.Name.ApiNameToHexId(_cryptoService);

            var collection = GetApiResourceCollection();

            var document = new ApiResourceDocument()
            {
                Id = id,
                BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(apiResource))
            };

            await collection.InsertOneAsync(document);
        }

        async public Task AddIdentityResourceAsync(IdentityResource identityResource)
        {
            if (identityResource == null)
            {
                return;
            }

            if (await FindApiResourceAsync(identityResource.Name) != null)
            {
                throw new Exception("Identity resource alread exists");
            }

            string id = identityResource.Name.IdentityNameToHexId(_cryptoService);

            var collection = GetIdentityResourceCollection();

            var document = new IdentityResourceDocument()
            {
                Id = id,
                BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(identityResource))
            };

            await collection.InsertOneAsync(document);
        }

        async public Task RemoveApiResourceAsync(ApiResource apiResource)
        {
            var collection = GetApiResourceCollection();

            string id = apiResource.Name.ApiNameToHexId(_cryptoService);

            await collection
                .DeleteOneAsync<ApiResourceDocument>(d => d.Id == id);
        }

        async public Task RemoveIdentityResourceAsync(IdentityResource identityResource)
        {
            var collection = GetIdentityResourceCollection();

            string id = identityResource.Name.IdentityNameToHexId(_cryptoService);

            await collection
                .DeleteOneAsync<IdentityResourceDocument>(d => d.Id == id);
        }

        async public Task UpdateApiResourceAsync(ApiResource apiResource, IEnumerable<string> propertyNames = null)
        {
            var collection = GetApiResourceCollection();

            string id = apiResource.Name.ApiNameToHexId(_cryptoService);

            UpdateDefinition<ApiResourceDocument> update = Builders<ApiResourceDocument>
                .Update
                .Set("BlobData", _cryptoService.EncryptText(_blobSerializer.SerializeObject(apiResource)));

            await collection
                .UpdateOneAsync<ApiResourceDocument>(d => d.Id == id, update);
        }

        async public Task UpdateIdentityResourceAsync(IdentityResource identityResource, IEnumerable<string> propertyNames = null)
        {
            var collection = GetIdentityResourceCollection();

            string id = identityResource.Name.IdentityNameToHexId(_cryptoService);

            UpdateDefinition<IdentityResourceDocument> update = Builders<IdentityResourceDocument>
                .Update
                .Set("BlobData", _cryptoService.EncryptText(_blobSerializer.SerializeObject(identityResource)));

            await collection
                .UpdateOneAsync<IdentityResourceDocument>(d => d.Id == id, update);
        }

        #endregion

        #region Helper

        private IMongoCollection<ApiResourceDocument> GetApiResourceCollection()
        {
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<ApiResourceDocument>(_apiCollectionName);

            return collection;
        }

        private IMongoCollection<IdentityResourceDocument> GetIdentityResourceCollection()
        {
            var client = new MongoClient(_connectionString);
            var database = client.GetDatabase(_databaseName);
            var collection = database.GetCollection<IdentityResourceDocument>(_identityCollectionName);

            return collection;
        }

        #endregion
    }
}
