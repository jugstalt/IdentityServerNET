using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.ErrorHandling;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class IndexModel : EditUserPageModel
{
    public IndexModel(
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IRoleDbContext roleDbContext = null)
        : base(userDbContext, userDbContextConfiguration, roleDbContext)
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
        string userId = Request.Form["__current_admin_account_user_id"].ToString();
        base.Category = String.IsNullOrWhiteSpace(Request.Form["_category"].ToString()) ?
              "Profile" :
              Request.Form["_category"].ToString();

        return await base.SecureHandlerAsync(async () =>
        {
            await base.LoadCurrentApplicationUserAsync(userId);

            if (this.CurrentApplicationUser == null)
            {
                throw new StatusMessageException($"Unable to load user.");
            }

            if (!this.EditorInfos.Validate(Request.Form, out string message))
            {
                throw new StatusMessageException($"Error: {message}");
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

            if (_userDbContext is IErrorMessage && ((IErrorMessage)_userDbContext).HasErrors)
            {
                throw new StatusMessageException(((IErrorMessage)_userDbContext).LastErrorMessage);
            }
        }
       , onFinally: () => RedirectToPage(new { id = userId, category = this.Category })
       , successMessage: $"The current users {Category.ToLower()} settings has been updated");
    }
}
