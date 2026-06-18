using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage;

public class TwoFactorAuthenticationModel : ManageAccountPageModel
{
    private const string AuthenicatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<TwoFactorAuthenticationModel> _logger;
    private readonly IConfiguration _configuration;

    public TwoFactorAuthenticationModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ILogger<TwoFactorAuthenticationModel> logger,
        IUserStoreFactory userStoreFactory,
        IConfiguration configuration)
        : base(userStoreFactory)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _configuration = configuration;
    }

    public bool HasAuthenticator { get; set; }

    public int RecoveryCodesLeft { get; set; }

    [BindProperty]
    public bool Is2faEnabled { get; set; }

    public bool IsMachineRemembered { get; set; }

    public bool PasskeySecondFactorAllowed { get; set; }

    public int PasskeyCount { get; set; }

    public bool CanUseRecoveryCodes { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGet()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null;
        Is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        IsMachineRemembered = await _signInManager.IsTwoFactorClientRememberedAsync(user);
        RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user);
        PasskeySecondFactorAllowed = _configuration.AllowPasskeySecondFactor();
        if (PasskeySecondFactorAllowed)
            PasskeyCount = (await _userManager.GetPasskeysAsync(user)).Count;

        CanUseRecoveryCodes = Is2faEnabled || (PasskeySecondFactorAllowed && PasskeyCount > 0);

        return Page();
    }

    public async Task<IActionResult> OnPost()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        await _signInManager.ForgetTwoFactorClientAsync();
        StatusMessage = "The current browser has been forgotten. When you login again from this browser you will be prompted for your 2fa code.";
        return RedirectToPage();
    }
}