using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
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
        protected IUserDbContext _userDbContext;

        protected ManageAccountProfilePageModel(
            IUserDbContext userManager,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration)
        {
            _userDbContext = userManager;
            EditorInfos =
                userDbContextConfiguration?.Value?.ManageAccountEditor;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public ApplicationUser ApplicationUser { get; set; } 

        public string Category { get; set; }

        public ManageAccountEditor EditorInfos { get; set; }
        
        async protected Task LoadUserAsync()
        {
            this.ApplicationUser = await _userDbContext.FindByNameAsync(User.Identity?.Name, new System.Threading.CancellationToken());
        }
    }
}
