using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class RemoveScopeModel : EditApiResourceModel
    {
        public RemoveScopeModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string scopeName)
        {
            await LoadCurrentApiResourceAsync(id);

            if (this.CurrentApiResource.Scopes != null &&
                this.CurrentApiResource.Scopes.Where(s => s.Name == scopeName).Count() >= 0)
            {
                this.CurrentApiResource.Scopes = this.CurrentApiResource.Scopes
                                                        .Where(s => s.Name != scopeName)
                                                        .ToArray();

                await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource);
            }

            return RedirectToPage("Scopes", new { id = id });

        }
    }
}
