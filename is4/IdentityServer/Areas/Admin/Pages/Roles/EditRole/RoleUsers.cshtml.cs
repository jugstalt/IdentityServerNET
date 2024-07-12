using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole;

public class RoleUsersModel : EditRolePageModel
{
    private readonly SignInManager<ApplicationUser> _signInManager;

    public RoleUsersModel(
        SignInManager<ApplicationUser> signInManager,
        IRoleDbContext roleDbContext,
        IUserDbContext userDbContext,
        IOptions<RoleDbContextConfiguration> roleDbContextConfiguration = null)
        : base(roleDbContext, roleDbContextConfiguration)
    {
        _signInManager = signInManager;
        _userDbContext = userDbContext as IUserRoleDbContext;
    }

    private IUserRoleDbContext _userDbContext;

    public IEnumerable<ApplicationUser> RoleUsers;

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        public string RoleId { get; set; }

        [DisplayName("Username or Email")]
        public string Username { get; set; }
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApplicationRoleAsync(id);

            if (_userDbContext == null)
            {
                throw new StatusMessageException("IUserRoleDbContext is not implemented with current user database");
            }

            this.RoleUsers = await _userDbContext.GetUsersInRoleAsync(CurrentApplicationRole.Name, CancellationToken.None);

            Input = new InputModel()
            {
                RoleId = CurrentApplicationRole.Id
            };
        }
        , onFinally: () => Page()
        , successMessage: "");
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApplicationRoleAsync(Input.RoleId);

            if (String.IsNullOrWhiteSpace(Input.Username))
            {
                throw new StatusMessageException("Please type a correct username");
            }

            if (_userDbContext == null)
            {
                throw new StatusMessageException("IUserRoleDbContext is not implemented with current user database");
            }

            var user = await _userDbContext.FindByNameAsync(Input.Username.ToUpper(), CancellationToken.None);
            if (user == null)
            {
                user = await _userDbContext.FindByEmailAsync(Input.Username.ToUpper(), CancellationToken.None);
            }

            if (user == null)
            {
                throw new StatusMessageException($"Unknown user {Input.Username}");
            }

            await _userDbContext.AddToRoleAsync(user, CurrentApplicationRole.Name, CancellationToken.None);
            await _signInManager.RefreshSignInAsync(user);
        }
        , onFinally: () => RedirectToPage(new { id = Input.RoleId })
        , successMessage: $"User {Input.Username} successfully added to role"
        , onException: (ex) => Page());
    }

    async public Task<IActionResult> OnGetRemoveAsync(string id, string userId)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApplicationRoleAsync(id);

            if (_userDbContext == null)
            {
                throw new StatusMessageException("IUserRoleDbContext is not implemented with current user database");
            }

            var user = await _userDbContext.FindByIdAsync(userId, CancellationToken.None);

            if (user == null)
            {
                throw new StatusMessageException($"Unknown user");
            }

            await _userDbContext.RemoveFromRoleAsync(user, CurrentApplicationRole.Name, CancellationToken.None);
            await _signInManager.RefreshSignInAsync(user);
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: "Successfully removed user");
    }
}
