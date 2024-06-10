using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class PropertiesModel : EditApiResourcePageModel
    {
        public PropertiesModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string option, bool value)
        {
            await LoadCurrentApiResourceAsync(id);

            this.Input = new InputModel()
            {
                ApiName = this.CurrentApiResource.Name,
                ApiResource = this.CurrentApiResource
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            List<string> propertyNames = new List<string>();
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentApiResourceAsync(Input.ApiName);

                var inputClient = Input.ApiResource;

                foreach (var propertyInfo in typeof(Client).GetProperties())
                {

                    if (!Input.IgnoreProperties.Contains(propertyInfo.Name) &&
                        propertyInfo.CanWrite &&
                        propertyInfo.CanRead &&
                        (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(int)))
                    {
                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            if ((propertyInfo.GetValue(inputClient) == null && !String.IsNullOrEmpty((string)propertyInfo.GetValue(this.CurrentApiResource))) ||
                                (propertyInfo.GetValue(inputClient) != null && !propertyInfo.GetValue(inputClient).Equals(propertyInfo.GetValue(this.CurrentApiResource))))
                            {
                                propertyInfo.SetValue(this.CurrentApiResource, propertyInfo.GetValue(inputClient));
                                propertyNames.Add(propertyInfo.Name);
                            }
                        }
                        if (propertyInfo.PropertyType == typeof(int))
                        {
                            if (!propertyInfo.GetValue(this.CurrentApiResource).Equals(propertyInfo.GetValue(inputClient)))
                            {
                                propertyInfo.SetValue(this.CurrentApiResource, propertyInfo.GetValue(inputClient));
                                propertyNames.Add(propertyInfo.Name);
                            }
                        }
                    }
                }

                if (propertyNames.Count > 0)
                {
                    await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, propertyNames);
                    this.StatusMessage = $"Properties '{String.Join(", ", propertyNames)}' updated successfully";
                }
                else
                {
                    throw new StatusMessageException("No changes found...");
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.ApiName })
            , successMessage: "");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ApiName { get; set; }
            public ApiResourceModel ApiResource { get; set; }
            public string[] IgnoreProperties => new string[] { "Name", "DisplayName", "Description" };
        }
    }
}
