using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Abstractions.Services.Serialize;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext;

public class FileBlobResourceDb : IResourceDbContextModify
{
    protected string _rootPath = null;
    private ICryptoService _cryptoService = null;
    private IBlobSerializer _blobSerializer = null;

    public FileBlobResourceDb(IOptions<ResourceDbContextConfiguration> options)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("FileBlobResourceDb: no connection string defined");
        }

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
            if (options.Value.InitialIdentityResources != null)
            {

            }
        }
    }

    #region IResourceDbContext

    async public Task AddApiResourceAsync(ApiResourceModel apiResource)
    {
        string id = apiResource.Name.NameToHexId(_cryptoService);
        FileInfo fi = new FileInfo($"{_rootPath}/{id}.api");

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

    async public Task<ApiResourceModel> FindApiResourceAsync(string name)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{name.NameToHexId(_cryptoService)}.api");

        if (!fi.Exists)
        {
            return null;
        }

        using (var reader = File.OpenText(fi.FullName))
        {
            var fileText = await reader.ReadToEndAsync();
            fileText = _cryptoService.DecryptText(fileText);

            return _blobSerializer.DeserializeObject<ApiResourceModel>(fileText);
        }
    }

    async public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
    {
        List<ApiResourceModel> apiResources = new List<ApiResourceModel>();

        foreach (var scopeName in scopeNames)
        {
            if (new FileInfo($"{_rootPath}/{scopeName.NameToHexId(_cryptoService)}.api").Exists)
            {
                apiResources.Add(await FindApiResourceAsync(scopeName));
            }
        }

        return apiResources;
    }

    public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{apiResource.Name.NameToHexId(_cryptoService)}.api");

        if (fi.Exists)
        {
            fi.Delete();
        }

        return Task.CompletedTask;
    }

    async public Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string> propertyNames = null)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{apiResource.Name.NameToHexId(_cryptoService)}.api");

        if (fi.Exists)
        {
            fi.Delete();
        }

        await AddApiResourceAsync(apiResource);
    }

    async public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
    {
        List<ApiResourceModel> apiResources = new List<ApiResourceModel>();

        foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.api"))
        {
            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();
                fileText = _cryptoService.DecryptText(fileText);

                apiResources.Add(_blobSerializer.DeserializeObject<ApiResourceModel>(fileText));
            }
        }

        return apiResources;
    }

    async public Task<IdentityResourceModel> FindIdentityResource(string name)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{name.NameToHexId(_cryptoService)}.identity");

        if (!fi.Exists)
        {
            return null;
        }

        using (var reader = File.OpenText(fi.FullName))
        {
            var fileText = await reader.ReadToEndAsync();
            fileText = _cryptoService.DecryptText(fileText);

            return _blobSerializer.DeserializeObject<IdentityResourceModel>(fileText);
        }
    }

    async public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
    {
        List<IdentityResourceModel> identityResources = new List<IdentityResourceModel>();

        foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.identity"))
        {
            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();
                fileText = _cryptoService.DecryptText(fileText);

                identityResources.Add(_blobSerializer.DeserializeObject<IdentityResourceModel>(fileText));
            }
        }

        return identityResources;
    }

    async public Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        string id = identityResource.Name.NameToHexId(_cryptoService);
        FileInfo fi = new FileInfo($"{_rootPath}/{id}.identity");

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

    async public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string> propertyNames)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{identityResource.Name.NameToHexId(_cryptoService)}.identity");

        if (fi.Exists)
        {
            fi.Delete();
        }

        await AddIdentityResourceAsync(identityResource);
    }

    public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{identityResource.Name.NameToHexId(_cryptoService)}.identity");

        if (fi.Exists)
        {
            fi.Delete();
        }

        return Task.CompletedTask;
    }

    #endregion
}
