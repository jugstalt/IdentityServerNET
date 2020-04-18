using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        async public Task<IActionResult> OnGetRemoveAsync(string id, string userClaim)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(id);

                this.CurrentIdentityResource.UserClaims = this.CurrentIdentityResource
                                                        .UserClaims
                                                        .Where(c => c != userClaim)
                                                        .ToArray();

                await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource, new[] { "UserClaims" });
            }
            , onFinally: () => RedirectToPage(new { id = id })
            , successMessage: $"Successfully removed user claim '{ userClaim }'");
        }

        async public Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            return await SecureHandlerAsync(async () =>
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

                        await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource, new[] { "UserClaims" });
                    }
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.IdentityName })
            , successMessage: "User claims successfully updated");
        }

        [BindProperty]
        public NewUserClaimModel Input { get; set; }

        public class NewUserClaimModel
        {
            public string IdentityName { get; set; }

            [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
            public string UserClaim { get; set; }
        }
    }
}
