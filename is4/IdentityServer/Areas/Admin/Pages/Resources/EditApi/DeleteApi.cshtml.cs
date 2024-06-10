using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

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
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.ApiName);

                if (Input.ConfirmApiName == CurrentApiResource.Name)
                {
                    await _resourceDb.RemoveApiResourceAsync(this.CurrentApiResource);
                }
                else
                {
                    throw new Exception("Please type the correct api resource name");
                }
            }
            , onFinally: () => RedirectToPage("../Apis")
            , successMessage: "Api resource successfully deleted"
            , onException: (ex) => RedirectToPage(new { id = Input.ApiName }));
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
