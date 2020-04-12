using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class IndexModel : EditApiResourcePageModel
    {
        public IndexModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentApiResourceAsync(id);

            Input = new InputModel()
            {
                Name = CurrentApiResource.Name,
                DisplayName = CurrentApiResource.DisplayName,
                Decription = CurrentApiResource.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.Name);

                CurrentApiResource.DisplayName = Input.DisplayName;
                CurrentApiResource.Description = Input.Decription;

                await _resourceDb.UpdateApiResourceAsync(CurrentApiResource);

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
