using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class RemoveUserClaimModel : EditIdentityResourcePageModel
    {
        public RemoveUserClaimModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string userClaim)
        {
            await LoadCurrentIdentityResourceAsync(id);

            this.CurrentIdentityResource.UserClaims = this.CurrentIdentityResource
                                                    .UserClaims
                                                    .Where(c => c != userClaim)
                                                    .ToArray();

            await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource);

            return RedirectToPage("UserClaims", new { id = id });

        }
    }
}
