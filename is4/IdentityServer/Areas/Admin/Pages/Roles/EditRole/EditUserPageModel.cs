using IdentityServer.Legacy;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Models;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.UserInteraction;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole
{
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
            this.CurrentApplicationRole = await _roleDbContext.FindByIdAsync(id, CancellationToken.None);
        }

        public string Category { get; set; }

        public ApplicationRole CurrentApplicationRole { get; set; }
    }
}
