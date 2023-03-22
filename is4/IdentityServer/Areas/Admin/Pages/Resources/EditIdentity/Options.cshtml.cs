using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditIdentity
{
    public class OptionsModel : EditIdentityResourcePageModel
    {
        public OptionsModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentIdentityResourceAsync(id);

            return Page();
        }

        async public Task<IActionResult> OnGetSetAsync(string id, string option, bool value)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentIdentityResourceAsync(id);

                var property = this.CurrentIdentityResource.GetType().GetProperty(option);
                if (property != null)
                {
                    property.SetValue(this.CurrentIdentityResource, value);
                    await _resourceDb.UpdateIdentityResourceAsync(this.CurrentIdentityResource, new[] { option });
                }
            }
            , onFinally: () => RedirectToPage(new { id = id })
            , successMessage: "");
        }
    }
}
