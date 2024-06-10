using IdentityServer.Nova.Models;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker
{
    public interface IEditLockerPageModel
    {
        SecretsLocker CurrentLocker { get; set; }
    }
}
