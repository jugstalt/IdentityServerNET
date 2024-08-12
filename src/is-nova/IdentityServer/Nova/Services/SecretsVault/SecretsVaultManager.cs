using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Servivces.DbContext;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.SecretsVault;

public class SecretsVaultManager
{
    private readonly ISecretsVaultDbContext _secretsVaultDb;
    private readonly IVaultSecretCryptoService _vaultSecrtesCryptoService;

    public SecretsVaultManager(
        ISecretsVaultDbContext secretsVaultDb = null,
        IVaultSecretCryptoService vaultSecrtesCryptoService = null)
    {
        _secretsVaultDb = secretsVaultDb;
        _vaultSecrtesCryptoService = vaultSecrtesCryptoService;
    }

    async public Task<VaultSecretVersion> GetSecretVersion(string path)
    {
        if (_secretsVaultDb == null || _vaultSecrtesCryptoService == null)
        {
            throw new ArgumentException("Scretsvault not initialized. No SecretsVaultDbContext or VautlSecretCryptService definied");
        }

        string[] pathParts = path.Split('/');

        if (pathParts.Length < 2 || pathParts.Length > 3)
        {
            throw new StatusMessageException($"Invalid path: {path}");
        }

        VaultSecretVersion secretVersion = null;
        if (pathParts.Length == 3)
        {
            if (!long.TryParse(pathParts[2], out long versionTimeStamp))
            {
                throw new StatusMessageException($"Invalid version time stamp: {pathParts[2]}");
            }

            secretVersion = await _secretsVaultDb.GetSecretVersionAsync(pathParts[0], pathParts[1], versionTimeStamp, CancellationToken.None);
        }
        else
        {
            secretVersion = (await _secretsVaultDb.GetVaultSecretVersionsAsync(pathParts[0], pathParts[1], CancellationToken.None))
                                .OrderByDescending(s => s.VersionTimeStamp)
                                .FirstOrDefault();
        }

        if (secretVersion == null)
        {
            throw new StatusMessageException($"Secret {path} not found");
        }

        secretVersion.Secret =
            _vaultSecrtesCryptoService.DecryptText(secretVersion.Secret, Encoding.Unicode);

        return secretVersion;
    }
}
