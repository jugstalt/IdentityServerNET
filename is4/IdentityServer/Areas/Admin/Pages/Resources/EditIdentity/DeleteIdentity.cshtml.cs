using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class DeleteIdentityModel : EditIdentityResourcePageModel
    {
        public DeleteIdentityModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentIdentityResourceAsync(id);

            Input = new InputModel()
            {
                IdentityName = CurrentIdentityResource.Name
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(Input.IdentityName);

                if (Input.ConfirmIdentityName == CurrentIdentityResource.Name)
                {
                    await _resourceDb.RemoveIdentityResourceAsync(this.CurrentIdentityResource);
                    return RedirectToPage("../Identities");
                } else
                {
                    throw new Exception("Please type the correct identity resource name");
                }
            }, onException: (ex)=> RedirectToPage(new { id = this.CurrentIdentityResource.Name }));
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string IdentityName { get; set; }
            public string ConfirmIdentityName { get; set; }
        }
    }
}
