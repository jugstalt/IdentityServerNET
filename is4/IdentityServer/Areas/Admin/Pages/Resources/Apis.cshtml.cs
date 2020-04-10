using IdentityServer.Legacy.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class ApisModel : AdminPageModel
    {
        private IResourceDbContextModify _resourceDb = null;
        public ApisModel(IResourceDbContext clientDbContext)
        {
            _resourceDb = clientDbContext as IResourceDbContextModify;
        }

        async public Task<IActionResult> OnGetAsync()
        {
            if (_resourceDb != null)
            {
                this.ApiResources = await _resourceDb.GetAllApiResources();

                Input = new NewApiResource();
            }

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            // is valid client id
            string apiName = Input.ApiResourceName.Trim().ToLower();

            if (_resourceDb != null)
            {
                var apiResource = new ApiResource(apiName, Input.ApiResourceDisplayName);

                await _resourceDb.AddApiResourceAsync(apiResource);
            }

            return RedirectToPage("EditApi/Index", new { id = apiName });
        }

        public IEnumerable<ApiResource> ApiResources { get; set; }

        [BindProperty]
        public NewApiResource Input { get; set; }

        public class NewApiResource
        {
            public string ApiResourceName { get; set; }
            public string ApiResourceDisplayName { get; set; }
        }
    }
}
