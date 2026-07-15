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
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<LoginWith2faModel> _logger;
    private readonly IEventService _events;
    private readonly ILoginBotDetection _loginBotDetection;

    public LoginWith2faModel(
        SignInManager<ApplicationUser> signInManager,
        ILogger<LoginWith2faModel> logger,
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

    public bool RememberMe { get; set; }

    public string ReturnUrl { get; set; }

    public class InputModel
    {
        [Required]
        [StringLength(7, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Text)]
        [Display(Name = "Authenticator code")]
        public string TwoFactorCode { get; set; }

        [Display(Name = "Remember this machine")]
        public bool RememberMachine { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string returnUrl = null)
    {
        // Ensure the user has gone through the username & password screen first
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        ReturnUrl = returnUrl;
        RememberMe = rememberMe;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(bool rememberMe, string returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        returnUrl = returnUrl ?? Url.Content("~/");

        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
        {
            throw new InvalidOperationException($"Unable to load two-factor authentication user.");
        }

        // Same username-based fail tracking as the password step (AccountController.LoginPassword) -
        // repeated guesses against a 2FA code are already unlikely to succeed within Identity's own
        // lockout window, but the tar-pit delay adds friction from the first few failures, not just
        // after the lockout threshold.
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

        var authenticatorCode = Input.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

        var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, Input.RememberMachine);

        if (result.Succeeded)
        {
            if (_loginBotDetection != null)
                await _loginBotDetection.RemoveSuspiciousUserAsync(user.UserName);

            _logger.LogInformation("User with ID '{UserId}' logged in with 2fa.", user.Id);
            await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

            return LocalRedirect(returnUrl);
        }
        else if (result.IsLockedOut)
        {
            _logger.LogWarning("User with ID '{UserId}' account locked out.", user.Id);
            await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "account locked out"));

            return RedirectToPage("./Lockout");
        }
        else
        {
            if (_loginBotDetection != null)
                await _loginBotDetection.AddSuspiciousUserAsync(user.UserName);

            _logger.LogWarning("Invalid authenticator code entered for user with ID '{UserId}'.", user.Id);
            await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "invalid authenticator code entered"));

            ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
            return Page();
        }
    }
}
