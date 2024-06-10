using IdentityServer.Nova.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface ISecretsVaultDbContext
    {
        Task<bool> CreateLockerAsync(SecretsLocker secretsLocker, CancellationToken cancellationToken);
        Task<bool> CreateVaultSecretAsync(string lockerName, VaultSecret vaultSecret, CancellationToken cancellationToken);
        Task<bool> CreateVaultSecretVersionAsync(string lockerName, string valutSecretName, VaultSecretVersion vaultSecretVersion, CancellationToken cancellationToken);

        Task<IEnumerable<SecretsLocker>> GetLockersAsync(CancellationToken cancellationToken);
        Task<IEnumerable<VaultSecret>> GetVaultSecretsAsync(string lockerName, CancellationToken cancellationToken);
        Task<IEnumerable<VaultSecretVersion>> GetVaultSecretVersionsAsync(string lockerName, string vaultSecretName, CancellationToken cancellationToken);
        Task<VaultSecretVersion> GetSecretVersionAsync(string lockerName, string vaultSecretName, long versionStamp, CancellationToken cancellationToken);

        Task<bool> UpadteLockerAsync(SecretsLocker secretsLocker, CancellationToken cancellationToken);
        Task<bool> UpadteVaultSecretAsync(string lockerName, VaultSecret vaultSecret, CancellationToken cancellationToken);

        Task<bool> RemoveLockerAsync(string lockerName, CancellationToken cancellationToken);
        Task<bool> RemoveVaultSecretAsync(string lockerName, string vaultSecretName, CancellationToken cancellation);
        Task<bool> RemoveVaultSecretVersionAsync(string lockerName, string vaultSecretName, long versionStamp, CancellationToken cancellationToken);
    }
}
