using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class UserClaimsModel : EditIdentityResourcePageModel
    {
        public UserClaimsModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentIdentityResourceAsync(id);

            this.Input = new NewUserClaimModel()
            {
                IdentityName = CurrentIdentityResource.Name
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(Input.IdentityName);

                if (!String.IsNullOrWhiteSpace(Input.UserClaim))
                {
                    List<string> userClaims = new List<string>();
                    if (this.CurrentIdentityResource.UserClaims != null)
                    {
                        userClaims.AddRange(this.CurrentIdentityResource.UserClaims);
                    }

                    if (!userClaims.Contains(Input.UserClaim.ToLower()))
                    {
                        userClaims.Add(Input.UserClaim.ToLower());
                        this.CurrentIdentityResource.UserClaims = userClaims.ToArray();

                        await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource);
                    }
                }

                return RedirectToPage(new { id = Input.IdentityName });
            }, onException: (ex) => RedirectToPage(new { id = Input.IdentityName }));
        }

        [BindProperty]
        public NewUserClaimModel Input { get; set; }

        public class NewUserClaimModel
        {
            public string IdentityName { get; set; }
            public string UserClaim { get; set; }
        }
    }
}
