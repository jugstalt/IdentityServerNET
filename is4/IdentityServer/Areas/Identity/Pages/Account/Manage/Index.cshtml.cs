using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : ManageAccountPageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            SignInManager<ApplicationUser> signInManager,
            IUserDbContext userDbContext,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration = null)
            : base("Profile", userDbContext, userDbContextConfiguration)
        {
            _userDbContext = userDbContext;
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await base.LoadUserAsync();
            if (this.ApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await base.LoadUserAsync();

            if (this.ApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            if (!ModelState.IsValid)
            {
                await base.LoadUserAsync();
                return Page();
            }

            foreach(var formKey in this.Request.Form.Keys)
            {
                if (formKey.IndexOf("Input.Options.") == 0)
                {
                    var propertyName = formKey.Substring("Input.Options.".Length);

                    var optionalProperty = base.OptionalPropertyInfos?
                                               .PropertyInfos?
                                               .Where(p => p.Name == propertyName)
                                               .FirstOrDefault();

                    if (optionalProperty == null || optionalProperty.Action.HasFlag(DbPropertyInfoAction.ReadOnly))
                    {
                        continue;
                    }

                    await _userDbContext.UpdatePropertyAsync(
                        this.ApplicationUser, 
                        optionalProperty,
                        this.Request.Form[formKey].ToString(),
                        new System.Threading.CancellationToken());
                }
            }

            await _signInManager.RefreshSignInAsync(this.ApplicationUser);
            StatusMessage = "Your profile has been updated";
            return RedirectToPage();
        }
    }
}
