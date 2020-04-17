using IdentityServer.Legacy;
using IdentityServer.Legacy.UserInteraction;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Users.EditUser
{
    public interface IEditUserPageModel
    {
        public AdminAccountEditor EditorInfos {get;set;}
        public ApplicationUser CurrentApplicationUser { get; set; }
    }
}
