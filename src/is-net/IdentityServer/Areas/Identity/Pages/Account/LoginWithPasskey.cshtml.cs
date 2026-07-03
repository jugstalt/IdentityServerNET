#nullable enable

using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account;

/// <summary>
/// Passkey challenge page shown after username/password succeeds but
/// AllowPasskeySecondFactor is configured and the user has passkeys enrolled.
/// </summary>
[Microsoft.AspNetCore.Authorization.AllowAnonymous]
public class LoginWithPasskeyModel : PageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<LoginWithPasskeyModel> _logger;
    private readonly IEventService _events;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IRealmDbContext? _realmDb;

    public LoginWithPasskeyModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<LoginWithPasskeyModel> logger,
        IEventService events,
        IIdentityServerInteractionService interaction,
        IRealmDbContext? realmDb = null)
    {
        _signInManager = signInManager;
        _userManager   = userManager;
        _logger        = logger;
        _events        = events;
        _interaction   = interaction;
        _realmDb       = realmDb;
    }

    public string ReturnUrl { get; set; } = string.Empty;

    public bool HasAuthenticatorApp { get; set; }

    public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
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
    public async Task<IActionResult> OnPostAsync(string assertionJson, string? returnUrl = null)
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
        var assertResult = await _signInManager.PerformPasskeyAssertionAsync(assertionJson);

        if (!assertResult.Succeeded || assertResult.User == null)
        {
            _logger.LogWarning("Passkey second-factor assertion failed.");
            ModelState.AddModelError(string.Empty, "Passkey verification failed.");
            ReturnUrl = returnUrl;
            return Page();
        }

        var user = assertResult.User;

        // Realm guard — verify the user is allowed to access this client before signing in.
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (!await IsUserAllowedForClientAsync(user, context?.Client?.ClientId))
        {
            // Sign out the two-factor cookie so the partial-auth state is cleaned up.
            await HttpContext.SignOutAsync(IdentityConstants.TwoFactorUserIdScheme);
            await _events.RaiseAsync(new UserLoginFailureEvent(
                user.UserName, "realm access denied", clientId: context?.Client.ClientId));
            ModelState.AddModelError(string.Empty, "Your account is not permitted to access this application.");
            ReturnUrl = returnUrl;
            return Page();
        }

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

    private async Task<bool> IsUserAllowedForClientAsync(ApplicationUser user, string? clientId)
    {
        if (_realmDb is null || !clientId.HasRealm())
            return true;

        var email = string.IsNullOrEmpty(user.Email) ? user.UserName : user.Email;
        if (string.IsNullOrEmpty(email)) return false;

        int at = email!.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;

        var domain = email.Substring(at + 1).ToLowerInvariant();
        var realm = await _realmDb.FindByDomainAsync(domain, CancellationToken.None);
        return clientId.ClientAllowsUserRealm(realm?.Name);
    }
}
