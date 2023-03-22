using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

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
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(Input.Name);

                CurrentIdentityResource.DisplayName = Input.DisplayName;
                CurrentIdentityResource.Description = Input.Decription;

                await _resourceDb.UpdateIdentityResourceAsync(CurrentIdentityResource, new[] { "DisplayName", "Description" });
            }
            , onFinally: () => RedirectToPage(new { id = Input.Name })
            , successMessage: "Identity resource successfully updated");
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
