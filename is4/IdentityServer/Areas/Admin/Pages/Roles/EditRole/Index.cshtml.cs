using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace IdentityServer.Areas.Admin.Pages.Roles.EditRole
{
    public class IndexModel : EditRolePageModel
    {
        public IndexModel(
            IRoleDbContext roleDbContext,
            IOptions<RoleDbContextConfiguration> roleDbContextConfiguration = null)
            : base(roleDbContext, roleDbContextConfiguration)
        {

        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentApplicationRoleAsync(id);

            Input = new InputModel()
            {
                RoleId = CurrentApplicationRole.Id,
                RoleName = CurrentApplicationRole.Name,
                RoleDescription = CurrentApplicationRole.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentApplicationRoleAsync(Input.RoleId);

                CurrentApplicationRole.Description = await _roleDbContext.UpdatePropertyAsync(CurrentApplicationRole, "Description", Input.RoleDescription , CancellationToken.None);
            }
            , onFinally: () => RedirectToPage(new { id = Input.RoleId })
            , successMessage: "The client has been updated successfully");
        }


        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string RoleId { get; set; }

            [DisplayName("Name")]
            public string RoleName { get; set; }

            [DisplayName("Description")]
            public string RoleDescription { get; set; }
        }
    }
}