using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Exceptions;
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
    public AddUserModel AddUserInput { get; set; }

    public class AddUserModel
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

            AddUserInput = new AddUserModel()
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
            await LoadCurrentApplicationRoleAsync(AddUserInput.RoleId);

            if (String.IsNullOrWhiteSpace(AddUserInput.Username))
            {
                throw new StatusMessageException("Please type a correct username");
            }

            if (_userDbContext == null)
            {
                throw new StatusMessageException("IUserRoleDbContext is not implemented with current user database");
            }

            var user = await _userDbContext.FindByNameAsync(AddUserInput.Username.ToUpper(), CancellationToken.None);
            if (user == null)
            {
                user = await _userDbContext.FindByEmailAsync(AddUserInput.Username.ToUpper(), CancellationToken.None);
            }

            if (user == null)
            {
                throw new StatusMessageException($"Unknown user {AddUserInput.Username}");
            }

            await _userDbContext.AddToRoleAsync(user, CurrentApplicationRole.Name, CancellationToken.None);

            if (user.UserName.Equals(this.User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                // refresh credentials
                await _signInManager.RefreshSignInAsync(user);
            }
        }
        , onFinally: () => RedirectToPage(new { id = AddUserInput.RoleId })
        , successMessage: $"User {AddUserInput.Username} successfully added to role"
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

            if (user.UserName.Equals(this.User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                // refresh credentials
                await _signInManager.RefreshSignInAsync(user);
            }
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: "Successfully removed user");
    }
}
