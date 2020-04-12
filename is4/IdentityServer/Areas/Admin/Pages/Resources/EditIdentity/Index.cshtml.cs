using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class IndexModel : EditIdentityResourcePageModel
    {
        public IndexModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentIdentityResourceAsync(id);

            Input = new InputModel()
            {
                Name = CurrentIdentityResource.Name,
                DisplayName = CurrentIdentityResource.DisplayName,
                Decription = CurrentIdentityResource.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(Input.Name);

                CurrentIdentityResource.DisplayName = Input.DisplayName;
                CurrentIdentityResource.Description = Input.Decription;

                await _resourceDb.UpdateIdentityResourceAsync(CurrentIdentityResource);

                return Page();
            });
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string Name { get; set; }
            public string DisplayName { get; set; }
            public string Decription { get; set; }
        }
    }
}
