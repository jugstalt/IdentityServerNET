using IdentityServerNET.Abstractions.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi;

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
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApiResourceAsync(Input.Name);

            CurrentApiResource.DisplayName = Input.DisplayName;
            CurrentApiResource.Description = Input.Decription;

            await _resourceDb.UpdateApiResourceAsync(CurrentApiResource, new[] { "DisplayName", "Description" });
        }
        , onFinally: () => RedirectToPage(new { id = Input.Name })
        , successMessage: "Api resource successfully updated");
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
