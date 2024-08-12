using IdentityServer.Nova.Models;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret;

public interface IEditVaultSecretPageModel
{
    VaultSecret CurrentSecret { get; }

    string LockerName { get; }
}
