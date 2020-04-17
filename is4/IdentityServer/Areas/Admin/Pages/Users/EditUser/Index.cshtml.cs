using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser
{
    public class IndexModel : EditUserPageModel
    {
        public IndexModel(
            IUserDbContext userDbContext,
            IOptions<UserDbContextConfiguration> userDbContextConfiguration)
            : base(userDbContext, userDbContextConfiguration)
        {

        }

        async public Task<IActionResult> OnGetAsync(string id, string category)
        {
            this.Category = String.IsNullOrWhiteSpace(category) ? "Profile" : category;

            await base.LoadCurrentApplicationUserAsync(id);
            if (this.CurrentApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await base.LoadCurrentApplicationUserAsync(Request.Form["__current_admin_account_user_id"].ToString());
           
            if (this.CurrentApplicationUser == null)
            {
                return NotFound($"Unable to load user.");
            }

            base.Category = String.IsNullOrWhiteSpace(Request.Form["_category"].ToString()) ?
                "Profile" :
                Request.Form["_category"].ToString();

            if (!this.EditorInfos.Validate(Request.Form, out string message))
            {
                StatusMessage = $"Error: { message }";
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            foreach (var formKey in this.Request.Form.Keys)
            {
                var propertyName = formKey;

                var editorInfo = base.EditorInfos?
                                     .EditorInfos?
                                     .Where(p => p.Name == propertyName && this.Category.Equals(p.Category, StringComparison.OrdinalIgnoreCase))
                                     .FirstOrDefault();

                if (editorInfo == null || editorInfo.EditorType.HasFlag(EditorType.ReadOnly))
                {
                    continue;
                }

                await _userDbContext.UpdatePropertyAsync(
                    this.CurrentApplicationUser,
                    editorInfo,
                    this.Request.Form[formKey].ToString(),
                    new System.Threading.CancellationToken());
            }

            StatusMessage = $"The current users { Category.ToLower() } settings has been updated";
            return RedirectToPage(new { id=this.CurrentApplicationUser.Id, category = this.Category });
        }
    }
}
