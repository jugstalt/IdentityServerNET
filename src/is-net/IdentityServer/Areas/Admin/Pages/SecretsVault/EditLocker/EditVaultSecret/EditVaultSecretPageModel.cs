using IdentityServerNET.Models;
using IdentityServerNET.Servivces.DbContext;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret;

public class EditVaultSecretPageModel : SecurePageModel, IEditVaultSecretPageModel
{
    public EditVaultSecretPageModel(
        ISecretsVaultDbContext roleDbContext)
    {
        _secretsVaultDb = roleDbContext;
    }

    protected ISecretsVaultDbContext _secretsVaultDb = null;

    async protected Task LoadCurrentSecretAsync(string lockerName, string id)
    {
        this.LockerName = lockerName;

        var secret = (await _secretsVaultDb.GetVaultSecretsAsync(lockerName, CancellationToken.None))
                                    .Where(l => l.Name == id)
                                    .FirstOrDefault();

        this.CurrentSecret = secret != null && await this.IsInCurrentRealmAsync(lockerName)
            ? secret
            : null;
    }

    public string LockerName { get; private set; }
    public VaultSecret CurrentSecret { get; private set; }
}
