using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
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
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                var inputClient = Input.Client;
                var hasChanges = false;

                foreach (var propertyInfo in typeof(Client).GetProperties())
                {

                    if (!Input.IgnoreProperties.Contains(propertyInfo.Name) &&
                        propertyInfo.CanWrite &&
                        propertyInfo.CanRead &&
                        (propertyInfo.PropertyType == typeof(string) || propertyInfo.PropertyType == typeof(int)))
                    {
                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            if (!String.IsNullOrWhiteSpace(propertyInfo.GetValue(this.CurrentClient)?.ToString()) &&
                                !String.IsNullOrWhiteSpace(propertyInfo.GetValue(inputClient)?.ToString()) &&
                                !propertyInfo.GetValue(this.CurrentClient).Equals(propertyInfo.GetValue(inputClient)))
                            {
                                propertyInfo.SetValue(this.CurrentClient, propertyInfo.GetValue(inputClient));
                                hasChanges = true;
                            }
                        }
                        if (propertyInfo.PropertyType == typeof(int))
                        {
                            if (!propertyInfo.GetValue(this.CurrentClient).Equals(propertyInfo.GetValue(inputClient)))
                            {
                                propertyInfo.SetValue(this.CurrentClient, propertyInfo.GetValue(inputClient));
                                hasChanges = true;
                            }
                        }
                    }
                }

                if (hasChanges)
                {
                    await _clientDb.UpdateClientAsync(this.CurrentClient);
                }

                return Page();
            });
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ClientId { get; set; }
            public Client Client { get; set; }
            public string[] IgnoreProperties => new string[] { "ClientId", "ClientName", "Description" };
        }
    }
}
