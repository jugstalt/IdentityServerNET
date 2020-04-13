using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string apiName = Input.ApiResourceName.Trim().ToLower();

            return await PostFormHandlerAsync(async () =>
            {
                if (_resourceDb != null)
                {
                    var apiResource = new ApiResource(apiName, Input.ApiResourceDisplayName);

                    await _resourceDb.AddApiResourceAsync(apiResource);
                }
            }
            , onFinally: () => RedirectToPage("EditApi/Index", new { id = apiName })
            , successMessage: "Api resource successfully created"
            , onException: (ex) => RedirectToPage());
        }

        public IEnumerable<ApiResource> ApiResources { get; set; }

        [BindProperty]
        public NewApiResource Input { get; set; }

        public class NewApiResource
        {
            [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
            public string ApiResourceName { get; set; }
            public string ApiResourceDisplayName { get; set; }
        }
    }
}
