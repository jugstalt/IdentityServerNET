using IdentityServer.Legacy;
using IdentityServer.Legacy.UserInteraction;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole
{
    public interface IEditRolePageModel
    {
        public ApplicationRole CurrentApplicationRole { get; set; }
    }
}
