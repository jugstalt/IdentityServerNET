using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class SetOptionModel : EditApiResourcePageModel
    {
        public SetOptionModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string option, bool value)
        {
            await LoadCurrentApiResourceAsync(id);

            var property = this.CurrentApiResource.GetType().GetProperty(option);
            if (property != null)
            {
                property.SetValue(this.CurrentApiResource, value);
                await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource);
            }

            return RedirectToPage("Options", new { id = id });
        }
    }
}
