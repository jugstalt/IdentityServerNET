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
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public class ManageAccountProfilePageModel : PageModel, IManageAccountPageModel
    {
        protected IUserStoreFactory _userStoreFactory;

        protected ManageAccountProfilePageModel(IUserStoreFactory userStoreFactory)
        {
            _userStoreFactory = userStoreFactory;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public ApplicationUser ApplicationUser { get; set; } 

        public string Category { get; set; }

        async public Task<ManageAccountEditor> EditorInfos() => (await _userStoreFactory.CreateUserDbContextInstance())?.ContextConfiguration?.ManageAccountEditor;

        async protected Task<IUserDbContext> CurrentUserDbContext() => await _userStoreFactory.CreateUserDbContextInstance();

        async protected Task LoadUserAsync()
        {
            var userDbContext = await _userStoreFactory.CreateUserDbContextInstance();
            this.ApplicationUser = await userDbContext.FindByNameAsync(User.Identity?.Name, new System.Threading.CancellationToken());
        }
    }
}
