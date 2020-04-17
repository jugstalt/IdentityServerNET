using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Users
{
    public class IndexModel : PageModel
    {
        private IUserDbContext _userDb = null;
        public IndexModel(IUserDbContext userDbContext)
        {
            _userDb = userDbContext as IUserDbContext;
        }

        public IEnumerable<ApplicationUser> ApplicationUsers { get; set; }

        async public Task<IActionResult> OnGetAsync(int skip = 0)
        {
            if (_userDb is IAdminUserDbContext)
            {
                this.ApplicationUsers = await ((IAdminUserDbContext)_userDb).GetUsersAsync(100, skip, CancellationToken.None);
            }

            return Page();
        }
    }
}
