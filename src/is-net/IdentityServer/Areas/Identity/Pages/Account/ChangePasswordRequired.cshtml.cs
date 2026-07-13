using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account;

/// <summary>
/// Forced password-change gate for one-time/temporary passwords. Reached only via
/// <c>IdentityServer.Net.Middleware.MustChangePasswordMiddleware</c>, which redirects here on every
/// request from a signed-in user whose <see cref="ApplicationUser.MustChangePassword"/> flag is set.
/// </summary>
public class ChangePasswordRequiredModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<ChangePasswordRequiredModel> _logger;

    public ChangePasswordRequiredModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<ChangePasswordRequiredModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current (temporary) password")]
        public string OldPassword { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm new password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public IActionResult OnGet(string returnUrl = null)
    {
        ReturnUrl = returnUrl ?? Url.Content("~/");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");
        ReturnUrl = returnUrl;

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var changePasswordResult = await _userManager.ChangePasswordAsync(user, Input.OldPassword, Input.NewPassword);
        if (!changePasswordResult.Succeeded)
        {
            foreach (var error in changePasswordResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return Page();
        }

        user.MustChangePassword = false;
        await _userManager.UpdateAsync(user);

        // Reissue the auth cookie so the middleware re-checks the (now cleared) flag on the next request.
        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User '{UserId}' completed the forced one-time-password change.", user.Id);

        return LocalRedirect(returnUrl);
    }
}
