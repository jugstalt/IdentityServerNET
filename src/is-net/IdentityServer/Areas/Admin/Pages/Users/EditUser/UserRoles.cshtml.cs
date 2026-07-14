using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class UserRolesModel : EditUserPageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    //private readonly SignInManager<ApplicationUser> _signInManager;

    public UserRolesModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IRoleDbContext roleDbContext = null,
        IRealmUserScope realmUserScope = null)
        : base(userDbContext, userDbContextConfiguration, roleDbContext, realmUserScope)
    {
        _userManager = userManager;
        //_signInManager = signInManager;
    }

    public string[] UserRoles;
    public bool IsRoleAdministrator = false;
    public IEnumerable<ApplicationRole> AddableRoles = null;

    async public Task<IActionResult> OnGetAsync(string id)
    {
        IsRoleAdministrator = (await _userManager.GetUserAsync(this.User)).IsRoleAdministrator();

        await LoadCurrentApplicationUserAsync(id);
        if (CurrentApplicationUser == null)
        {
            return NotFound($"Unable to load user.");
        }

        if (IsRoleAdministrator && _roleDbContext is IAdminRoleDbContext adminRoleDb)
        {
            var realmRoles = await GetRolesForTargetRealmAsync(adminRoleDb);

            AddableRoles = realmRoles
                .Where(r => CurrentApplicationUser.Roles?.Any() != true || !CurrentApplicationUser.Roles.Contains(r.Name));
        }

        return Page();
    }

    async public Task<IActionResult> OnGetRemoveAsync(string id, string roleName)
    {
        return await SecureHandlerAsync(async () =>
        {
            if (!(await _userManager.GetUserAsync(this.User)).IsRoleAdministrator())
            {
                throw new StatusMessageException("No allowed");
            }

            await LoadCurrentApplicationUserAsync(id);
            if (CurrentApplicationUser == null)
            {
                throw new StatusMessageException("Unable to load user.");
            }

            await ((IUserRoleDbContext)_userDbContext).RemoveFromRoleAsync(CurrentApplicationUser, roleName, CancellationToken.None);
            //await _signInManager.RefreshSignInAsync(CurrentApplicationUser);
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: $"Role {roleName} removed");
    }

    async public Task<IActionResult> OnGetAddAsync(string id, string roleName)
    {
        return await SecureHandlerAsync(async () =>
        {
            if (!(await _userManager.GetUserAsync(this.User)).IsRoleAdministrator())
            {
                throw new StatusMessageException("No allowed");
            }

            await LoadCurrentApplicationUserAsync(id);
            if (CurrentApplicationUser == null)
            {
                throw new StatusMessageException("Unable to load user.");
            }

            // Defense in depth: reject a role that isn't actually assignable for this user's realm, even
            // if it was submitted directly (crafted URL) rather than picked from the filtered list —
            // otherwise a realm admin could grant a global system role (e.g. realm-administrator) to
            // any user they can edit.
            if (_roleDbContext is IAdminRoleDbContext adminRoleDb)
            {
                var realmRoles = await GetRolesForTargetRealmAsync(adminRoleDb);
                if (!realmRoles.Any(r => (r.Name ?? r.Id) == roleName))
                {
                    throw new StatusMessageException($"Role '{roleName}' is not available for this user.");
                }
            }

            await ((IUserRoleDbContext)_userDbContext).AddToRoleAsync(CurrentApplicationUser, roleName, CancellationToken.None);
            //await _signInManager.RefreshSignInAsync(CurrentApplicationUser);
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: $"Role {roleName} added");
    }

    // Roles must be listed/validated for the EDITED USER's realm, not the caller's own — otherwise a
    // system admin editing a realm admin's roles (or a realm admin somehow reaching another realm's
    // user) would see/grant the wrong realm's roles, including the global system ones.
    private async Task<IEnumerable<ApplicationRole>> GetRolesForTargetRealmAsync(IAdminRoleDbContext adminRoleDb)
    {
        if (_realmUserScope == null)
        {
            return await adminRoleDb.GetRolesAsync(1000, 0, CancellationToken.None);
        }

        var targetRealm = await _realmUserScope.GetUserRealmAsync(CurrentApplicationUser, CancellationToken.None);

        using (RealmScopeOverride.Begin(targetRealm))
        {
            return await adminRoleDb.GetRolesAsync(1000, 0, CancellationToken.None);
        }
    }
}
