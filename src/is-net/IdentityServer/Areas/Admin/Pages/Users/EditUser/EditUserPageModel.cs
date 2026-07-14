using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public class EditUserPageModel : SecurePageModel, IEditUserPageModel
{
    public EditUserPageModel(
        IUserDbContext userDbContext,
        IOptions<UserDbContextConfiguration> userDbContextConfiguration,
        IRoleDbContext roleDbContext,
        IRealmUserScope realmUserScope = null)
    {
        _userDbContext = userDbContext;
        _roleDbContext = roleDbContext;
        _realmUserScope = realmUserScope;
        EditorInfos =
                userDbContextConfiguration?.Value?.AdminAccountEditor;
    }

    public AdminAccountEditor EditorInfos { get; set; }

    protected IUserDbContext _userDbContext = null;
    protected IRoleDbContext _roleDbContext = null;
    protected IRealmUserScope _realmUserScope = null;

    public bool HasRoleDbContext => _roleDbContext != null && _userDbContext is IUserRoleDbContext;

    // Central realm guard for every EditUser subpage (Profile, Email, Roles, Set Password, Reset 2FA,
    // Delete): a realm admin may only load users of its own realm; the system admin only users of no
    // realm plus realm admin accounts specifically (same rule as the Users list — see IRealmUserScope).
    // Without this, any of these pages could be reached for an arbitrary user by id, regardless of realm.
    async protected Task LoadCurrentApplicationUserAsync(string id)
    {
        var user = await _userDbContext.FindByIdAsync(id, CancellationToken.None);

        if (user != null && _realmUserScope != null)
        {
            var visible = await _realmUserScope.FilterToCurrentRealmAsync(new[] { user }, CancellationToken.None);
            user = visible.Any() ? user : null;
        }

        this.CurrentApplicationUser = user;
    }

    public string Category { get; set; }

    public ApplicationUser CurrentApplicationUser { get; set; }
}
