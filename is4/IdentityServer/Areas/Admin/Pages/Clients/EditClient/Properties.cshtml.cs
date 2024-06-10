using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
{
    public class PropertiesModel : EditClientPageModel
    {
        public PropertiesModel(IClientDbContext clientDbContext)
            : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string option, bool value)
        {
            await LoadCurrentClientAsync(id);

            this.Input = new InputModel()
            {
                ClientId = this.CurrentClient.ClientId,
                Client = this.CurrentClient
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            List<string> propertyNames = new List<string>();
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                var inputClient = Input.Client;

                foreach (var propertyInfo in typeof(ClientModel).GetProperties())
                {

                    if (!Input.IgnoreProperties.Contains(propertyInfo.Name) &&
                        propertyInfo.CanWrite &&
                        propertyInfo.CanRead &&
                        (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(int)))
                    {
                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            if ((propertyInfo.GetValue(inputClient) == null && !String.IsNullOrEmpty((string)propertyInfo.GetValue(this.CurrentClient))) ||
                                (propertyInfo.GetValue(inputClient) != null && !propertyInfo.GetValue(inputClient).Equals(propertyInfo.GetValue(this.CurrentClient))))
                            {
                                propertyInfo.SetValue(this.CurrentClient, propertyInfo.GetValue(inputClient));
                                propertyNames.Add(propertyInfo.Name);
                            }
                        }
                        if (propertyInfo.PropertyType == typeof(int))
                        {
                            if (!propertyInfo.GetValue(this.CurrentClient).Equals(propertyInfo.GetValue(inputClient)))
                            {
                                propertyInfo.SetValue(this.CurrentClient, propertyInfo.GetValue(inputClient));
                                propertyNames.Add(propertyInfo.Name);
                            }
                        }
                    }
                }

                if (propertyNames.Count > 0)
                {
                    await _clientDb.UpdateClientAsync(this.CurrentClient, propertyNames);
                    this.StatusMessage = $"Properties '{String.Join(", ", propertyNames)}' updated successfully";
                }
                else
                {
                    throw new Exception("No properties found to update");
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.ClientId })
            , successMessage: "");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ClientId { get; set; }
            public ClientModel Client { get; set; }
            public string[] IgnoreProperties => new string[] { "ClientId", "ClientName", "Description" };
        }
    }
}
