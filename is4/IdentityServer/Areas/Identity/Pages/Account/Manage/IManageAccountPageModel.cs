using IdentityServer.Legacy.Services.DbContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public interface IManageAccountPageModel
    {
        ManageAccountDbPropertyInfos OptionalPropertyInfos { get; set; }
    }
}
