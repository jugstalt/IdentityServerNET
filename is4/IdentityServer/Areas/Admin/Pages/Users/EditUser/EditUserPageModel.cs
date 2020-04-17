using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser
{
    public class EditUserPageModel : PageModel, IEditUserPageModel
    {
        public EditUserPageModel(
            IUserDbContext userDbContext,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration)
        {
            _userDbContext = userDbContext;
            EditorInfos =
                    userDbContextConfiguration?.Value?.AdminAccountEditor;
        }

        public AdminAccountEditor EditorInfos { get; set; }

        protected IUserDbContext _userDbContext = null;

        async protected Task LoadCurrentApplicationUserAsync(string id)
        {
            this.CurrentApplicationUser = await _userDbContext.FindByIdAsync(id, CancellationToken.None);
        }

        public string Category { get; set; }

        [TempData]
        public String StatusMessage { get; set; }

        public ApplicationUser CurrentApplicationUser { get; set; }
    }
}
