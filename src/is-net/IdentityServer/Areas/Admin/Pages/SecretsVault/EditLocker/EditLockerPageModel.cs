using IdentityServerNET.Models;
using IdentityServerNET.Servivces.DbContext;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker;

public class EditLockerPageModel : SecurePageModel, IEditLockerPageModel
{
    public EditLockerPageModel(
        ISecretsVaultDbContext roleDbContext)
    {
        _secretsVaultDb = roleDbContext;
    }

    protected ISecretsVaultDbContext _secretsVaultDb = null;

    async protected Task LoadCurrentLockerAsync(string id)
    {
        var locker = (await _secretsVaultDb.GetLockersAsync(CancellationToken.None))
                                    .Where(l => l.Name == id)
                                    .FirstOrDefault();

        this.CurrentLocker = locker != null && await this.IsInCurrentRealmAsync(locker.Name)
            ? locker
            : null;
    }

    public SecretsLocker CurrentLocker { get; set; }
}
