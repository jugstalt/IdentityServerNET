using IdentityServer.Legacy;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public class ManageAccountPageModel : PageModel, IManageAccountPageModel
    {
        protected IUserStoreFactory _userStoreFactory;

        protected ManageAccountPageModel(IUserStoreFactory userStoreFactory)
        {
            _userStoreFactory = userStoreFactory;
        }

        async public Task<ManageAccountEditor> EditorInfos() => (await _userStoreFactory.CreateUserDbContextInstance())?.ContextConfiguration?.ManageAccountEditor;
    }
}
