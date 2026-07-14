using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class ResetSecondFactorModel : EditUserPageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ResetSecondFactorModel> _logger;
    private readonly IUserPasskeyStore<ApplicationUser> _passkeyStore;

    public ResetSecondFactorModel(
        UserManager<ApplicationUser> userManager,
        ILogger<ResetSecondFactorModel> logger,
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IUserPasskeyStore<ApplicationUser> passkeyStore = null,
        IRoleDbContext roleDbContext = null,
        IRealmUserScope realmUserScope = null)
        : base(userDbContext, userDbContextConfiguration, roleDbContext, realmUserScope)
    {
        _userManager  = userManager;
        _logger       = logger;
        _passkeyStore = passkeyStore;
    }

    [BindProperty]
    public string UserId { get; set; }

    public bool HasAuthenticatorApp { get; set; }

    public int PasskeyCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await base.LoadCurrentApplicationUserAsync(id);
        if (CurrentApplicationUser == null)
            return NotFound($"Unable to load user with ID '{id}'.");

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound($"Unable to load user with ID '{id}'.");

        UserId              = id;
        HasAuthenticatorApp = await _userManager.GetTwoFactorEnabledAsync(user)
                              && await _userManager.GetAuthenticatorKeyAsync(user) != null;

        if (_passkeyStore != null)
            PasskeyCount = (await _userManager.GetPasskeysAsync(user)).Count;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await base.SecureHandlerAsync(async () =>
        {
            await base.LoadCurrentApplicationUserAsync(UserId);
            if (CurrentApplicationUser == null)
                throw new StatusMessageException($"Unable to load user with ID '{UserId}'.");

            var user = await _userManager.FindByIdAsync(UserId);
            if (user == null)
                throw new StatusMessageException($"Unable to load user with ID '{UserId}'.");

            // Disable the authenticator app and rotate its secret key.
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);

            // Remove all registered passkeys.
            if (_passkeyStore != null)
            {
                var passkeys = await _userManager.GetPasskeysAsync(user);
                foreach (var pk in passkeys)
                    await _passkeyStore.RemovePasskeyAsync(user, pk.CredentialId, CancellationToken.None);
            }

            // Clear any active lockout so the user is not inadvertently blocked.
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            _logger.LogWarning(
                "Admin reset second factor for user '{UserId}' ('{UserName}').",
                user.Id, user.UserName);
        },
        onFinally: () => RedirectToPage(new { id = UserId }),
        successMessage: "Second factor reset. The user will need to re-enrol two-factor authentication on their next login.");
    }
}
