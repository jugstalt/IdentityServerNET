using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy;
using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Users
{
    public class IndexModel : SecurePageModel
    {
        private IUserDbContext _userDb = null;
        public IndexModel(IUserDbContext userDbContext)
        {
            _userDb = userDbContext as IUserDbContext;
        }

        public IEnumerable<ApplicationUser> ApplicationUsers { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Username { get; set; }
        }

        async public Task<IActionResult> OnGetAsync(int skip = 0)
        {
            if (_userDb is IAdminUserDbContext)
            {
                this.ApplicationUsers = await ((IAdminUserDbContext)_userDb).GetUsersAsync(100, skip, CancellationToken.None);
            }

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            string userId=String.Empty;

            return await SecureHandlerAsync(async () =>
            {
                var user = await _userDb.FindByNameAsync(Input.Username?.ToString(), CancellationToken.None);
                if(user==null)
                {
                    throw new StatusMessageException($"Unknown user { Input.Username }");
                }

                userId = user.Id;
            },
            onFinally: () => RedirectToPage("EditUser/Index", new { id = userId }),
            successMessage: "",
            onException: (ex) => RedirectToPage());
        }
    }
}
