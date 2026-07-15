using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginWithRecoveryCodeModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginWithRecoveryCodeModel> _logger;
    private readonly IEventService _events;
    private readonly ILoginBotDetection _loginBotDetection;

    public LoginWithRecoveryCodeModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginWithRecoveryCodeModel> logger,
        IEventService events,
        ILoginBotDetection loginBotDetection = null)
    {
        _signInManager = signInManager;
        _logger = logger;
        _events = events;
        _loginBotDetection = loginBotDetection;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [BindProperty]
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Recovery Code")]
        public string RecoveryCode { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        // Same username-based fail tracking as the password step (AccountController.LoginPassword).
        if (_loginBotDetection != null && await _loginBotDetection.IsSuspiciousUserAsync(user.UserName))
        {
            try
            {
                await _loginBotDetection.BlockSuspicousUser(user.UserName);
            }
            catch (StatusMessageException sme)
            {
                ModelState.AddModelError(string.Empty, sme.Message);
                return Page();
            }
        }

        var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);

        var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

        if (result.Succeeded)
        {
            if (_loginBotDetection != null)
                await _loginBotDetection.RemoveSuspiciousUserAsync(user.UserName);

            _logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", user.Id);
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

            return LocalRedirect(returnUrl ?? Url.Content("~/"));
        }

        if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
            await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "account locked out"));

            return RedirectToPage("./Lockout");
        }
        else
        {
            if (_loginBotDetection != null)
                await _loginBotDetection.AddSuspiciousUserAsync(user.UserName);

            _logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}' ", user.Id);
            await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "Invalid recovery code entered "));
            ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");

            return Page();
        }
    }
}
