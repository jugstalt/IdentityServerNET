using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class UserEmailModel : EditUserPageModel
{
    public UserEmailModel(
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

        [Display(Name = "Email address")]
        [ReadOnly(true)]
        public string Email { get; set; }

        [Display(Name = "Is Confirmed")]
        public bool IsEmailConfirmed { get; set; }
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
            Email = this.CurrentApplicationUser.Email,
            IsEmailConfirmed = this.CurrentApplicationUser.EmailConfirmed
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await base.SecureHandlerAsync(async () =>
        {
            await base.LoadCurrentApplicationUserAsync(Input.CurrentUserId);

            await _userDbContext.UpdatePropertyAsync<bool>(this.CurrentApplicationUser, "EmailConfirmed", Input.IsEmailConfirmed, CancellationToken.None);
        }
        , onFinally: () => RedirectToPage(new { id = Input.CurrentUserId })
        , successMessage: "Email confirmation status changed");
    }
}
