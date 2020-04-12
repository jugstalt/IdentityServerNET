using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class IdentitiesModel : AdminPageModel
    {
        private IResourceDbContextModify _resourceDb = null;
        public IdentitiesModel(IResourceDbContext clientDbContext)
        {
            _resourceDb = clientDbContext as IResourceDbContextModify;
        }

        async public Task<IActionResult> OnGetAsync()
        {
            if (_resourceDb != null)
            {
                this.IdentityResources = await _resourceDb.GetAllIdentityResources();
                
                Input = new NewIdentityResource();
            }

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                // is valid client id
                string identityName = Input.IdentityResourceName.Trim().ToLower();

                if (_resourceDb != null)
                {
                    var identityResource = new IdentityResource()
                    {
                        Name = Input.IdentityResourceName,
                        DisplayName = Input.IdentityResourceDisplayName
                    };

                    await _resourceDb.AddIdentityResourceAsync(identityResource);
                }

                return RedirectToPage("EditIdentity/Index", new { id = identityName });
            });
        }

        public IEnumerable<IdentityResource> IdentityResources { get; set; }

        [BindProperty]
        public NewIdentityResource Input { get; set; }

        public class NewIdentityResource
        {
            public string IdentityResourceName { get; set; }
            public string IdentityResourceDisplayName { get; set; }
        }
    }
}
