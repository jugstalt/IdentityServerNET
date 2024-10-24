using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Services.Cryptography;
using IdentityServerNET.Services.Serialize;
using IdentityServerNET.Servivces.DbContext;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class FileBlobSecretsVaultDb : ISecretsVaultDbContext
{
    private readonly string _rootPath = null;
    private readonly ICryptoService _cryptoService = null;
    private readonly IBlobSerializer _blobSerializer = null;

    public FileBlobSecretsVaultDb(
        IOptions<SecretsVaultDbContextConfiguration> options)
    {
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("FileBlobSecretsVaultDb: no connection string defined");
        }

        _rootPath = options.Value.ConnectionString;
        _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
        _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

        var di = new DirectoryInfo(_rootPath);
        if (!di.Exists)
        {
            di.Create();
        }
    }

    #region ISecretsVaultDbContext

    async public Task<bool> CreateLockerAsync(SecretsLocker secretsLocker, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(secretsLocker?.Name))
        {
            throw new StatusMessageException("Invalid locker name");
        }

        var di = new DirectoryInfo($"{_rootPath}/{secretsLocker.Name}");
        if (di.Exists)
        {
            throw new StatusMessageException($"Locker {secretsLocker.Name} already exists");
        }

        di.Create();

        byte[] buffer = Encoding.UTF8.GetBytes(
            _cryptoService.EncryptText(_blobSerializer.SerializeObject(secretsLocker)));

        using (var fs = new FileStream($"{di.FullName}/_item.meta", FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return true;
    }

    async public Task<bool> CreateVaultSecretAsync(string lockerName, VaultSecret vaultSecret, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(lockerName))
        {
            throw new StatusMessageException("Invalid locker name");
        }

        if (String.IsNullOrWhiteSpace(vaultSecret?.Name))
        {
            throw new StatusMessageException("Invalid secret name");
        }

        var diLocker = new DirectoryInfo($"{_rootPath}/{lockerName}");
        if (!diLocker.Exists)
        {
            throw new StatusMessageException($"Locker {lockerName} not exists");
        }

        var di = new DirectoryInfo($"{diLocker.FullName}/{vaultSecret.Name}");
        if (di.Exists)
        {
            throw new StatusMessageException($"Secret {vaultSecret.Name} already exists in locker {lockerName}");
        }

        di.Create();

        byte[] buffer = Encoding.UTF8.GetBytes(
            _cryptoService.EncryptText(_blobSerializer.SerializeObject(vaultSecret)));

        using (var fs = new FileStream($"{di.FullName}/_item.meta", FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return true;
    }

    async public Task<bool> CreateVaultSecretVersionAsync(string lockerName, string valutSecretName, VaultSecretVersion vaultSecretVersion, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(lockerName))
        {
            throw new StatusMessageException("Invalid locker name");
        }

        if (String.IsNullOrWhiteSpace(valutSecretName))
        {
            throw new StatusMessageException("Invalid secret name");
        }

        var diLocker = new DirectoryInfo($"{_rootPath}/{lockerName}");
        if (!diLocker.Exists)
        {
            throw new StatusMessageException($"Locker {lockerName} not exists");
        }

        var diSecret = new DirectoryInfo($"{diLocker.FullName}/{valutSecretName}");
        if (!diSecret.Exists)
        {
            throw new StatusMessageException($"Secret {lockerName} not exists");
        }

        var fi = new FileInfo($"{diSecret.FullName}/{vaultSecretVersion.VersionTimeStamp}.secret");
        if (fi.Exists)
        {
            throw new StatusMessageException($"This version {vaultSecretVersion.VersionTimeStamp} already exists");
        }

        byte[] buffer = Encoding.UTF8.GetBytes(
            _cryptoService.EncryptText(_blobSerializer.SerializeObject(vaultSecretVersion)));

        using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return true;
    }

    async public Task<IEnumerable<SecretsLocker>> GetLockersAsync(CancellationToken cancellationToken)
    {
        List<SecretsLocker> secretLockers = new List<SecretsLocker>();

        foreach (var di in new DirectoryInfo(_rootPath).GetDirectories())
        {
            var fi = new FileInfo($"{di.FullName}/_item.meta");
            if (fi.Exists)
            {
                using (var reader = File.OpenText(fi.FullName))
                {
                    var fileText = await reader.ReadToEndAsync();
                    fileText = _cryptoService.DecryptText(fileText);

                    secretLockers.Add(_blobSerializer.DeserializeObject<SecretsLocker>(fileText));
                }
            }
        }

        return secretLockers;
    }

    async public Task<IEnumerable<VaultSecret>> GetVaultSecretsAsync(string lockerName, CancellationToken cancellationToken)
    {
        List<VaultSecret> vaultSecrets = new List<VaultSecret>();

        foreach (var di in new DirectoryInfo($"{_rootPath}/{lockerName}").GetDirectories())
        {
            var fi = new FileInfo($"{di.FullName}/_item.meta");
            if (fi.Exists)
            {
                using (var reader = File.OpenText(fi.FullName))
                {
                    var fileText = await reader.ReadToEndAsync();
                    fileText = _cryptoService.DecryptText(fileText);

                    vaultSecrets.Add(_blobSerializer.DeserializeObject<VaultSecret>(fileText));
                }
            }
        }

        return vaultSecrets;
    }

    async public Task<IEnumerable<VaultSecretVersion>> GetVaultSecretVersionsAsync(string lockerName, string vaultSecretName, CancellationToken cancellationToken)
    {
        List<VaultSecretVersion> vaultSecretVersions = new List<VaultSecretVersion>();

        foreach (var fi in new DirectoryInfo($"{_rootPath}/{lockerName}/{vaultSecretName}").GetFiles("*.secret"))
        {

            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();
                fileText = _cryptoService.DecryptText(fileText);

                vaultSecretVersions.Add(_blobSerializer.DeserializeObject<VaultSecretVersion>(fileText));
            }
        }

        return vaultSecretVersions;
    }

    async public Task<VaultSecretVersion> GetSecretVersionAsync(string lockerName, string vaultSecretName, long versionStamp, CancellationToken cancellationToken)
    {
        var fi = new FileInfo($"{_rootPath}/{lockerName}/{vaultSecretName}/{versionStamp}.secret");
        if (!fi.Exists)
        {
            throw new StatusMessageException($"Secretversion {lockerName}/{vaultSecretName}/{versionStamp} not exists.");
        }

        using (var reader = File.OpenText(fi.FullName))
        {
            var fileText = await reader.ReadToEndAsync();
            fileText = _cryptoService.DecryptText(fileText);

            return _blobSerializer.DeserializeObject<VaultSecretVersion>(fileText);
        }
    }

    public Task<bool> RemoveLockerAsync(string lockerName, CancellationToken cancellationToken)
    {
        var di = new DirectoryInfo($"{_rootPath}/{lockerName}");
        if (!di.Exists)
        {
            throw new StatusMessageException($"Locker {lockerName} not exists");
        }

        di.Delete(true);

        return Task.FromResult(true);
    }

    public Task<bool> RemoveVaultSecretAsync(string lockerName, string vaultSecretName, CancellationToken cancellation)
    {
        var di = new DirectoryInfo($"{_rootPath}/{lockerName}/{vaultSecretName}");
        if (!di.Exists)
        {
            throw new StatusMessageException($"Secret {lockerName}/{vaultSecretName} not exists");
        }

        di.Delete(true);

        return Task.FromResult(true);
    }

    public Task<bool> RemoveVaultSecretVersionAsync(string lockerName, string vaultSecretName, long versionStamp, CancellationToken cancellationToken)
    {
        var fi = new FileInfo($"{_rootPath}/{lockerName}/{vaultSecretName}/{versionStamp}.secret");
        if (!fi.Exists)
        {
            throw new StatusMessageException($"Secret version {lockerName}/{vaultSecretName}/{versionStamp} not exists");
        }

        fi.Delete();

        return Task.FromResult(true);
    }

    async public Task<bool> UpadteLockerAsync(SecretsLocker secretsLocker, CancellationToken cancellationToken)
    {
        var fi = new FileInfo($"{_rootPath}/{secretsLocker.Name}/_item.meta");
        if (!fi.Exists)
        {
            throw new StatusMessageException($"Locker {secretsLocker.Name} not exists");
        };

        byte[] buffer = Encoding.UTF8.GetBytes(
           _cryptoService.EncryptText(_blobSerializer.SerializeObject(secretsLocker)));

        fi.Delete();

        using (var fs = new FileStream($"{fi.FullName}", FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return true;
    }

    async public Task<bool> UpadteVaultSecretAsync(string lockerName, VaultSecret vaultSecret, CancellationToken cancellationToken)
    {
        var fi = new FileInfo($"{_rootPath}/{lockerName}/{vaultSecret.Name}/_item.meta");
        if (!fi.Exists)
        {
            throw new StatusMessageException($"Secret {lockerName}/{vaultSecret.Name} not exists");
        };

        byte[] buffer = Encoding.UTF8.GetBytes(
           _cryptoService.EncryptText(_blobSerializer.SerializeObject(vaultSecret)));

        fi.Delete();

        using (var fs = new FileStream($"{fi.FullName}", FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }

        return true;
    }

    #endregion
}
