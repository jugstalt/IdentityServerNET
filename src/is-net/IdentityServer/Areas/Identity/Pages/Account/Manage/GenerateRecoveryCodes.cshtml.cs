using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Identity.Pages.Account.Manage;

public class GenerateRecoveryCodesModel : ManageAccountPageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GenerateRecoveryCodesModel> _logger;
    private readonly IConfiguration _configuration;

    public GenerateRecoveryCodesModel(
        UserManager<ApplicationUser> userManager,
        ILogger<GenerateRecoveryCodesModel> logger,
        IUserStoreFactory userStoreFactory,
        IConfiguration configuration)
        : base(userStoreFactory)
    {
        _userManager   = userManager;
        _logger        = logger;
        _configuration = configuration;
    }

    [TempData]
    public string[] RecoveryCodes { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var hasPasskeyFactor   = _configuration.AllowPasskeySecondFactor()
            && (await _userManager.GetPasskeysAsync(user)).Count > 0;

        if (!isTwoFactorEnabled && !hasPasskeyFactor)
        {
            var userId = await _userManager.GetUserIdAsync(user);
            throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' because they do not have 2FA enabled.");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var isTwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        var userId             = await _userManager.GetUserIdAsync(user);
        var hasPasskeyFactor   = _configuration.AllowPasskeySecondFactor()
            && (await _userManager.GetPasskeysAsync(user)).Count > 0;

        if (!isTwoFactorEnabled && !hasPasskeyFactor)
        {
            throw new InvalidOperationException($"Cannot generate recovery codes for user with ID '{userId}' as they do not have 2FA enabled.");
        }

        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        RecoveryCodes = recoveryCodes.ToArray();

        _logger.LogInformation("User with ID '{UserId}' has generated new 2FA recovery codes.", userId);
        StatusMessage = "You have generated new recovery codes.";
        return RedirectToPage("./ShowRecoveryCodes");
    }
}