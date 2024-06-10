using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(Input.IdentityName);

                if (Input.ConfirmIdentityName == CurrentIdentityResource.Name)
                {
                    await _resourceDb.RemoveIdentityResourceAsync(this.CurrentIdentityResource);
                }
                else
                {
                    throw new StatusMessageException("Please type the correct identity resource name");
                }
            }
            , onFinally: () => RedirectToPage("../Identities")
            , successMessage: "Identity resource successfully deleted"
            , onException: (ex) => RedirectToPage(new { id = Input.IdentityName }));
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
