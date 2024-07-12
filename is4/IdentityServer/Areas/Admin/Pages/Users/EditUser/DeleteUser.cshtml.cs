using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class DeleteUserModel : EditUserPageModel
{
    public DeleteUserModel(
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IRoleDbContext roleDbContext = null)
        : base(userDbContext, userDbContextConfiguration, roleDbContext)
    {
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [HiddenInput]
        public string CurrentUserId { get; set; }

        [Display(Name = "Confirm Username")]
        public string ConfirmUsername { get; set; }
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await base.LoadCurrentApplicationUserAsync(id);
        if (this.CurrentApplicationUser == null)
        {
            return NotFound($"Unable to load user.");
        }

        this.Input = new InputModel()
        {
            CurrentUserId = this.CurrentApplicationUser.Id,
        };

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await base.SecureHandlerAsync(async () =>
        {
            await base.LoadCurrentApplicationUserAsync(Input.CurrentUserId);

            #region Verify Username

            if (!this.CurrentApplicationUser.UserName.Equals(Input.ConfirmUsername))
            {
                throw new StatusMessageException("Please type the correct username.");
            }


            #endregion

            await _userDbContext.DeleteAsync(this.CurrentApplicationUser, CancellationToken.None);
        }
        , onFinally: () => RedirectToPage("../Index")
        , successMessage: ""
        , onException: (ex) => RedirectToPage(new { id = Input.CurrentUserId }));
    }
}
