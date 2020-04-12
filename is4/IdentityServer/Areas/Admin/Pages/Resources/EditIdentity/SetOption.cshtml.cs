using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class SetOptionModel : EditIdentityResourcePageModel
    {
        public SetOptionModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string option, bool value)
        {
            await LoadCurrentIdentityResourceAsync(id);

            var property = this.CurrentIdentityResource.GetType().GetProperty(option);
            if (property != null)
            {
                property.SetValue(this.CurrentIdentityResource, value);
                await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource);
            }

            return RedirectToPage("Options", new { id = id });
        }
    }
}
