using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class RemoveSecretModel : EditApiResourcePageModel
    {
        public RemoveSecretModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, int secretIndex, string secretHash)
        {
            await LoadCurrentApiResourceAsync(id);

            if (this.CurrentApiResource.ApiSecrets != null && secretIndex >= 0 && this.CurrentApiResource.ApiSecrets.Count() > secretIndex)
            {
                var deleteSecret = this.CurrentApiResource.ApiSecrets.ToArray()[secretIndex];
                if (deleteSecret.Value.ToSha256().StartsWith(secretHash))
                {
                    this.CurrentApiResource.ApiSecrets = this.CurrentApiResource.ApiSecrets
                                                                .Where(s => s != deleteSecret)
                                                                .ToArray();

                    await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource);
                }
            }

            return RedirectToPage("Secrets", new { id = id });

        }
    }
}
