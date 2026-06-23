using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ConfirmEmailChangeModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ConfirmEmailChangeModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [TempData]
    public string StatusMessage { get; set; }

    // Carried from the link via GET and back into POST via hidden fields
    [BindProperty(SupportsGet = true)]
    public string UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Email { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Code { get; set; }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current password")]
        public string Password { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        if (UserId == null || Email == null || Code == null)
            return RedirectToPage("/Index");

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
            return NotFound($"Unable to load user with ID '{UserId}'.");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (UserId == null || Email == null || Code == null)
            return RedirectToPage("/Index");

        var user = await _userManager.FindByIdAsync(UserId);
        if (user == null)
            return NotFound($"Unable to load user with ID '{UserId}'.");

        if (!ModelState.IsValid)
            return Page();

        // Verify the current password against the EXISTING hash (which uses the OLD email in the template)
        if (!await _userManager.CheckPasswordAsync(user, Input.Password))
        {
            ModelState.AddModelError("Input.Password", "Incorrect password.");
            return Page();
        }

        var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Code));
        var result = await _userManager.ChangeEmailAsync(user, Email, code);
        if (!result.Succeeded)
        {
            StatusMessage = "Error changing email.";
            return Page();
        }

        // In this app email and username are the same
        var setUserNameResult = await _userManager.SetUserNameAsync(user, Email);
        if (!setUserNameResult.Succeeded)
        {
            StatusMessage = "Error changing user name.";
            return Page();
        }

        // Re-hash the password with the updated user (new Email/UserName now in effect).
        // This ensures the stored hash stays valid when the template includes {email} or {username}.
        user.PasswordHash = _userManager.PasswordHasher.HashPassword(user, Input.Password);
        await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);
        StatusMessage = "Thank you for confirming your email change.";
        return Page();
    }
}
