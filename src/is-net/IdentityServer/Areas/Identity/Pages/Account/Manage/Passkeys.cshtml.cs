using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage;

public class PasskeysModel : ManageAccountPageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<PasskeysModel> _logger;
    private readonly IUserPasskeyStore<ApplicationUser> _passkeyStore;
    private readonly IConfiguration _configuration;

    public PasskeysModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<PasskeysModel> logger,
        IUserStoreFactory userStoreFactory,
        IConfiguration configuration,
        IUserPasskeyStore<ApplicationUser> passkeyStore = null)
        : base(userStoreFactory)
    {
        _userManager   = userManager;
        _signInManager = signInManager;
        _logger        = logger;
        _configuration = configuration;
        _passkeyStore  = passkeyStore;
    }

    [TempData]
    public string StatusMessage { get; set; }

    [TempData]
    public string[] RecoveryCodes { get; set; }

    public IList<UserPasskeyInfo> Passkeys { get; set; } = new List<UserPasskeyInfo>();

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
            return NotFound();

        Passkeys = await _userManager.GetPasskeysAsync(user);
        return Page();
    }

    /// <summary>Returns attestation-options JSON for the JS registration flow.</summary>
    public async Task<IActionResult> OnGetCreationOptionsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var entity = new PasskeyUserEntity
        {
            Id          = user.Id,
            Name        = user.UserName ?? user.Email,
            DisplayName = user.UserName ?? user.Email
        };
        var json = await _signInManager.MakePasskeyCreationOptionsAsync(entity);
        return Content(json, "application/json");
    }

    /// <summary>Saves the newly attested passkey credential.</summary>
    public async Task<IActionResult> OnPostRegisterAsync(string attestationJson, string passkeyName)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(attestationJson))
        {
            StatusMessage = "Error: No attestation data received.";
            return RedirectToPage();
        }

        var result = await _signInManager.PerformPasskeyAttestationAsync(attestationJson);
        if (!result.Succeeded)
        {
            StatusMessage = "Error: Passkey registration failed – " + result.Failure?.Message;
            _logger.LogWarning("Passkey attestation failed for user '{UserId}': {Error}", user.Id, result.Failure?.Message);
            return RedirectToPage();
        }


        if (_passkeyStore != null)
        {
        if (!string.IsNullOrWhiteSpace(passkeyName))
            result.Passkey.Name = passkeyName.Trim();

            await _passkeyStore.AddOrUpdatePasskeyAsync(user, result.Passkey, CancellationToken.None);
        }

        _logger.LogInformation("User '{UserId}' registered a new passkey.", user.Id);

        // Auto-generate recovery codes when the user has none yet and passkey second-factor is active.
        // This ensures passkey-only users always have a self-service recovery path if they lose their passkey.
        if (_configuration.AllowPasskeySecondFactor()
            && await _userManager.CountRecoveryCodesAsync(user) == 0)
        {
            var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            RecoveryCodes = codes.ToArray();
            StatusMessage = "Passkey registered successfully. Recovery codes have been generated — save them somewhere safe.";
            return RedirectToPage("./ShowRecoveryCodes");
        }

        StatusMessage = "Passkey registered successfully.";
        return RedirectToPage();
    }

    /// <summary>Removes the passkey identified by the Base64Url-encoded credential ID.</summary>
    public async Task<IActionResult> OnPostRemoveAsync(string credentialIdBase64Url)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        if (string.IsNullOrWhiteSpace(credentialIdBase64Url))
        {
            StatusMessage = "Error: No credential ID provided.";
            return RedirectToPage();
        }

        // credentialIdBase64Url comes from the hidden field in the remove form
        var credId = ConvertFromBase64Url(credentialIdBase64Url);


        if (_passkeyStore != null)
        {
            await _passkeyStore.RemovePasskeyAsync(user, credId, CancellationToken.None);
        }

        StatusMessage = "Passkey removed.";
        _logger.LogInformation("User '{UserId}' removed a passkey.", user.Id);
        return RedirectToPage();
    }

    private static byte[] ConvertFromBase64Url(string base64url)
    {
        var b64 = base64url.Replace('-', '+').Replace('_', '/');
        switch (b64.Length % 4)
        {
            case 2: b64 += "=="; break;
            case 3: b64 += "="; break;
        }
        return System.Convert.FromBase64String(b64);
    }
}
