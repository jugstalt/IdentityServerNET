using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account;

/// <summary>
/// Passkey challenge page shown after username/password succeeds but
/// AllowPasskeySecondFactor is configured and the user has passkeys enrolled.
/// </summary>
[AllowAnonymous]
public class LoginWithPasskeyModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginWithPasskeyModel> _logger;
    private readonly IEventService _events;

    public LoginWithPasskeyModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginWithPasskeyModel> logger,
        IEventService events)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
        _logger        = logger;
        _events        = events;
    }

    public string ReturnUrl { get; set; }

    public bool HasAuthenticatorApp { get; set; }

    public async Task<IActionResult> OnGetAsync(string returnUrl = null)
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null)
            throw new InvalidOperationException("Unable to load two-factor authentication user.");

        ReturnUrl = returnUrl ?? Url.Content("~/");
        HasAuthenticatorApp = await _userManager.GetTwoFactorEnabledAsync(user)
            && await _userManager.GetAuthenticatorKeyAsync(user) != null;
        return Page();
    }

    // Called by the hidden form submitted by passkey.js after assertion completes.
    public async Task<IActionResult> OnPostAsync(string assertionJson, string returnUrl = null)
    {
        returnUrl = returnUrl ?? Url.Content("~/");

        if (string.IsNullOrWhiteSpace(assertionJson))
        {
            ModelState.AddModelError(string.Empty, "No passkey response received.");
            ReturnUrl = returnUrl;
            return Page();
        }

        // PerformPasskeyAssertionAsync reads the stored challenge (written into the
        // TwoFactorUserId cookie by MakePasskeyRequestOptionsAsync), verifies the
        // credential signature, and returns the user who owns the passkey.
        // The allowCredentials list was already scoped to the 2FA user in
        // OnGetChallengeAsync, so assertResult.User is guaranteed to be that user.
        var assertResult = await _signInManager.PerformPasskeyAssertionAsync(assertionJson);

        if (!assertResult.Succeeded || assertResult.User == null)
        {
            _logger.LogWarning("Passkey second-factor assertion failed.");
            ModelState.AddModelError(string.Empty, "Passkey verification failed.");
            ReturnUrl = returnUrl;
            return Page();
        }

        var user = assertResult.User;

        // Complete the two-factor sign-in using the standard Identity cookie approach.
        await _signInManager.SignInAsync(user, isPersistent: false);
        _logger.LogInformation("User '{UserId}' completed passkey second-factor.", user.Id);
        await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

        return LocalRedirect(returnUrl);
    }

    // Returns assertion options JSON for the JavaScript challenge.
    public async Task<IActionResult> OnGetChallengeAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        // Scope the allowCredentials list to this specific user.
        var json = await _signInManager.MakePasskeyRequestOptionsAsync(user);
        return Content(json, "application/json");
    }
}
