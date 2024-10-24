using IdentityServerNET.Abstractions.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi;

public class OptionsModel : EditApiResourcePageModel
{
    public OptionsModel(IResourceDbContext resourceDbContext)
         : base(resourceDbContext)
    {
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentApiResourceAsync(id);

        return Page();
    }

    async public Task<IActionResult> OnGetSetAsync(string id, string option, bool value)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApiResourceAsync(id);

            var property = this.CurrentApiResource.GetType().GetProperty(option);
            if (property != null)
            {
                property.SetValue(this.CurrentApiResource, value);
                await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, new[] { option });
            }
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: "");
    }
}