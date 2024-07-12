using IdentityServer.Nova.Models;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole;

public interface IEditRolePageModel
{
    public ApplicationRole CurrentApplicationRole { get; set; }
}
