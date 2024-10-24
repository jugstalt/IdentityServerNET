using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser;

public interface IEditUserPageModel
{
    public AdminAccountEditor EditorInfos { get; set; }
    public ApplicationUser CurrentApplicationUser { get; set; }
}
