using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using Microsoft.Extensions.Options;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole;

public class EditRolePageModel : SecurePageModel, IEditRolePageModel
{
    public EditRolePageModel(
        IRoleDbContext roleDbContext,
        IOptions<RoleDbContextConfiguration> roleDbContextConfiguration)
    {
        _roleDbContext = roleDbContext;
    }

    protected IRoleDbContext _roleDbContext = null;

    async protected Task LoadCurrentApplicationRoleAsync(string id)
    {
        var role = await _roleDbContext.FindByIdAsync(id, CancellationToken.None);
        this.CurrentApplicationRole = role != null && await this.IsInCurrentRealmAsync(role.Name)
            ? role
            : null;
    }

    public string Category { get; set; }

    public ApplicationRole CurrentApplicationRole { get; set; }
}
