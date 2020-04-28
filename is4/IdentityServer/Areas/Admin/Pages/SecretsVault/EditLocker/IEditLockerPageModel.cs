using IdentityServer.Legacy;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.UserInteraction;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker
{
    public interface IEditLockerPageModel
    {
        SecretsLocker CurrentLocker { get; set; }
    }
}
