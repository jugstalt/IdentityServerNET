using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class DeleteApiModel : EditApiResourcePageModel
    {
        public DeleteApiModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentApiResourceAsync(id);

            Input = new InputModel()
            {
                ApiName = CurrentApiResource.Name
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCurrentApiResourceAsync(Input.ApiName);

            if (Input.ConfirmApiName == CurrentApiResource.Name)
            {
                await _resourceDb.RemoveApiResourceAsync(this.CurrentApiResource);
                return RedirectToPage("../Apis");
            }

            return RedirectToPage(new { id = this.CurrentApiResource.Name });
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ApiName { get; set; }
            public string ConfirmApiName { get; set; }
        }
    }
}
