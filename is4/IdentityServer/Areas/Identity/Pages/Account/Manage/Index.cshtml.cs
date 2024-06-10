using IdentityServer.Nova;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : ManageAccountProfilePageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;

        public IndexModel(
            SignInManager<ApplicationUser> signInManager,
            IUserStoreFactory userStoreFactory)
            : base(userStoreFactory)
        {
            _signInManager = signInManager;
        }

        public string Username { get; set; }

        public async Task<IActionResult> OnGetAsync(string category)
        {
            this.Category = String.IsNullOrWhiteSpace(category) ? "Profile" : category;

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
            base.Category = String.IsNullOrWhiteSpace(Request.Form["_category"].ToString()) ?
                "Profile" :
                Request.Form["_category"].ToString();

            if (this.ApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            if (!(await EditorInfos()).Validate(Request.Form, out string message))
            {
                StatusMessage = $"Error: {message}";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var formKey in this.Request.Form.Keys)
            {
                var propertyName = formKey;

                var editorInfo = (await EditorInfos())?
                                     .EditorInfos?
                                     .Where(p => p.Name == propertyName && this.Category.Equals(p.Category, StringComparison.OrdinalIgnoreCase))
                                     .FirstOrDefault();

                if (editorInfo == null || editorInfo.EditorType.HasFlag(EditorType.ReadOnly))
                {
                    continue;
                }

                var userDbContext = await CurrentUserDbContext();
                await userDbContext.UpdatePropertyAsync(
                    this.ApplicationUser,
                    editorInfo,
                    this.Request.Form[formKey].ToString(),
                    new System.Threading.CancellationToken());
            }

            await _signInManager.RefreshSignInAsync(this.ApplicationUser);
            StatusMessage = $"Your {Category.ToLower()} settings has been updated";
            return RedirectToPage(new { category = this.Category });
        }
    }
}
