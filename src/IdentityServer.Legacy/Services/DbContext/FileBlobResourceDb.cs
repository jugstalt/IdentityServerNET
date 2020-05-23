using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.Serialize;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class FileBlobResourceDb : IResourceDbContextModify
    {
        protected string _rootPath = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer = null;

        public FileBlobResourceDb(IOptions<ResourceDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("FileBlobResourceDb: no connection string defined");

            _rootPath = options.Value.ConnectionString;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            DirectoryInfo di = new DirectoryInfo(_rootPath);
            if (!di.Exists)
            {
                di.Create();

                // Initialize Api Resources
                if (options.Value.InitialApiResources != null)
                {
                    foreach (var apiResource in options.Value.InitialApiResources)
                    {
                        AddApiResourceAsync(apiResource).Wait();
                    }
                }
                // Initialize Identity Resources
                if(options.Value.InitialIdentityResources !=null)
                {

                }
            }
        }

        #region IResourceDbContext

        async public Task AddApiResourceAsync(ApiResource apiResource)
        {
            string id = apiResource.Name.NameToHexId(_cryptoService);
            FileInfo fi = new FileInfo($"{ _rootPath }/{ id }.api");

            if (fi.Exists)
            {
                throw new Exception("Api already exists");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(
                _cryptoService.EncryptText(_blobSerializer.SerializeObject(apiResource)));

            using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                            FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        async public Task<ApiResource> FindApiResourceAsync(string name)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ name.NameToHexId(_cryptoService) }.api");

            if (!fi.Exists)
            {
                return null;
            }

            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();
                fileText = _cryptoService.DecryptText(fileText);

                return _blobSerializer.DeserializeObject<ApiResource>(fileText);
            }
        }

        async public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            List<ApiResource> apiResources = new List<ApiResource>();

            foreach (var scopeName in scopeNames)
            {
                if (new FileInfo($"{ _rootPath }/{ scopeName.NameToHexId(_cryptoService) }.api").Exists)
                {
                    apiResources.Add(await FindApiResourceAsync(scopeName));
                }
            }

            return apiResources;
        }

        public Task RemoveApiResourceAsync(ApiResource apiResource)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ apiResource.Name.NameToHexId(_cryptoService) }.api");

            if (fi.Exists)
            {
                fi.Delete();
            }

            return Task.CompletedTask;
        }

        async public Task UpdateApiResourceAsync(ApiResource apiResource, IEnumerable<string> propertyNames = null)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ apiResource.Name.NameToHexId(_cryptoService) }.api");

            if (fi.Exists)
            {
                fi.Delete();
            }

            await AddApiResourceAsync(apiResource);
        }

        async public Task<IEnumerable<ApiResource>> GetAllApiResources()
        {
            List<ApiResource> apiResources = new List<ApiResource>();

            foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.api"))
            {
                using (var reader = File.OpenText(fi.FullName))
                {
                    var fileText = await reader.ReadToEndAsync();
                    fileText = _cryptoService.DecryptText(fileText);

                    apiResources.Add(_blobSerializer.DeserializeObject<ApiResource>(fileText));
                }
            }

            return apiResources;
        }

        async public Task<IdentityResource> FindIdentityResource(string name)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ name.NameToHexId(_cryptoService) }.identity");

            if (!fi.Exists)
            {
                return null;
            }

            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();
                fileText = _cryptoService.DecryptText(fileText);

                return _blobSerializer.DeserializeObject<IdentityResource>(fileText);
            }
        }

        async public Task<IEnumerable<IdentityResource>> GetAllIdentityResources()
        {
            List<IdentityResource> identityResources = new List<IdentityResource>();

            foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.identity"))
            {
                using (var reader = File.OpenText(fi.FullName))
                {
                    var fileText = await reader.ReadToEndAsync();
                    fileText = _cryptoService.DecryptText(fileText);

                    identityResources.Add(_blobSerializer.DeserializeObject<IdentityResource>(fileText));
                }
            }

            return identityResources;
        }

        async public Task AddIdentityResourceAsync(IdentityResource identityResource)
        {
            string id = identityResource.Name.NameToHexId(_cryptoService);
            FileInfo fi = new FileInfo($"{ _rootPath }/{ id }.identity");

            if (fi.Exists)
            {
                throw new Exception("Identity resource already exists");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(
                _cryptoService.EncryptText(_blobSerializer.SerializeObject(identityResource)));

            using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                            FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        async public Task UpdateIdentityResourceAsync(IdentityResource identityResource, IEnumerable<string> propertyNames)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ identityResource.Name.NameToHexId(_cryptoService) }.identity");

            if (fi.Exists)
            {
                fi.Delete();
            }

            await AddIdentityResourceAsync(identityResource);
        }

        public Task RemoveIdentityResourceAsync(IdentityResource identityResource)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ identityResource.Name.NameToHexId(_cryptoService) }.identity");

            if (fi.Exists)
            {
                fi.Delete();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
