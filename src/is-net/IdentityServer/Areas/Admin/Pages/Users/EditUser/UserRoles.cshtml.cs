using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
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
        IRoleDbContext roleDbContext = null)
        : base(userDbContext, userDbContextConfiguration, roleDbContext)
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

        if (IsRoleAdministrator && _roleDbContext is IAdminRoleDbContext)
        {
            AddableRoles = (await ((IAdminRoleDbContext)_roleDbContext).GetRolesAsync(1000, 0, CancellationToken.None))
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

            await ((IUserRoleDbContext)_userDbContext).AddToRoleAsync(CurrentApplicationUser, roleName, CancellationToken.None);
            //await _signInManager.RefreshSignInAsync(CurrentApplicationUser);
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: $"Role {roleName} added");
    }
}
