using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class FileBlobRealmDb : IRealmDbContext
{
    private readonly string _rootPath;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    public FileBlobRealmDb(
            IOptions<RealmDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer blobSerializer = null)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("FileBlobRealmDb: no connection string defined");
        }

        _rootPath = options.Value.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();

        DirectoryInfo di = new DirectoryInfo(_rootPath);
        if (!di.Exists)
        {
            di.Create();
        }
    }

    public async Task<RealmModel> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();

        FileInfo fi = new FileInfo($"{_rootPath}/{realmName.NameToHexId(_cryptoService)}.realm");
        if (!fi.Exists)
        {
            return null;
        }

        return await ReadAsync(fi);
    }

    public async Task<RealmModel> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();

        var all = await GetAllAsync(cancellationToken);

        return all.FirstOrDefault(
            r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<RealmModel> realms = new List<RealmModel>();

        foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.realm"))
        {
            realms.Add(await ReadAsync(fi));
        }

        return realms;
    }

    public async Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (await FindByNameAsync(realm.Name, cancellationToken) != null)
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        realm.CreateDate = realm.CreateDate == default ? DateTime.UtcNow : realm.CreateDate;

        await WriteAsync(realm, overwrite: false);
    }

    public async Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        if (await FindByNameAsync(realm.Name, cancellationToken) == null)
        {
            throw new InvalidOperationException($"Realm '{realm.Name}' does not exist.");
        }

        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        await WriteAsync(realm, overwrite: true);
    }

    public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var name = (realm.Name ?? "").Trim().ToLowerInvariant();

        FileInfo fi = new FileInfo($"{_rootPath}/{name.NameToHexId(_cryptoService)}.realm");
        if (fi.Exists)
        {
            fi.Delete();
        }

        return Task.CompletedTask;
    }

    private async Task<RealmModel> ReadAsync(FileInfo fi)
    {
        using (var reader = File.OpenText(fi.FullName))
        {
            var fileText = _cryptoService.DecryptText(await reader.ReadToEndAsync());
            return _blobSerializer.DeserializeObject<RealmModel>(fileText);
        }
    }

    private async Task WriteAsync(RealmModel realm, bool overwrite)
    {
        FileInfo fi = new FileInfo($"{_rootPath}/{realm.Name.NameToHexId(_cryptoService)}.realm");
        if (fi.Exists && overwrite)
        {
            fi.Delete();
        }

        byte[] buffer = Encoding.UTF8.GetBytes(
            _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm)));

        using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
