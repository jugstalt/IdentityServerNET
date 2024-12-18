﻿using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class EditUserPageModel : SecurePageModel, IEditUserPageModel
{
    public EditUserPageModel(
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IRoleDbContext roleDbContext)
    {
        _userDbContext = userDbContext;
        _roleDbContext = roleDbContext;
        EditorInfos =
                userDbContextConfiguration?.Value?.AdminAccountEditor;
    }

    public AdminAccountEditor EditorInfos { get; set; }

    protected IUserDbContext _userDbContext = null;
    protected IRoleDbContext _roleDbContext = null;

    public bool HasRoleDbContext => _roleDbContext != null && _userDbContext is IUserRoleDbContext;

    async protected Task LoadCurrentApplicationUserAsync(string id)
    {
        this.CurrentApplicationUser = await _userDbContext.FindByIdAsync(id, CancellationToken.None);
    }

    public string Category { get; set; }

    public ApplicationUser CurrentApplicationUser { get; set; }
}
