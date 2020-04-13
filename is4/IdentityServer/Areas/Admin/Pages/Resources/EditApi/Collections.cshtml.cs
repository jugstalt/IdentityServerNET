using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class CollectionsModel : EditApiResourcePageModel
    {
        public CollectionsModel(IResourceDbContext resourceDbContext)
            : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentApiResourceAsync(id);

            this.Input = new InputModel()
            {
                ApiName = this.CurrentApiResource.Name
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.ApiName);

                string[] values = Input.PropertyValue == null ?
                                    new string[0] :
                                    Input.PropertyValue
                                        .Replace("\r", "")
                                        .Split('\n')
                                        .Select(v => v.Trim())
                                        .Where(v => !String.IsNullOrEmpty(v))
                                        .ToArray();

                var propertyInfo = typeof(ApiResource).GetProperty(Input.PropertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(this.CurrentApiResource, values);
                    await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, new[] { Input.PropertyName });
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.ApiName })
            , successMessage: $"{ Input.PropertyName } successfully updated");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ApiName { get; set; }
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
            public string[] IgnoreProperties => new string[] { "" };
        }
    }
}
