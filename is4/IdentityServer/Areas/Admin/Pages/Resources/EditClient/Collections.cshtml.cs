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
    public class CollectionsModel : EditClientPageModel
    {
        public CollectionsModel(IClientDbContext clientDbContext)
            : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            this.Input = new InputModel()
            {
                ClientId = this.CurrentClient.ClientId
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                string[] values = Input.PropertyValue == null ?
                                   new string[0] :
                                   Input.PropertyValue
                                       .Replace("\r", "")
                                       .Split('\n')
                                       .Select(v => v.Trim())
                                       .Where(v => !String.IsNullOrEmpty(v))
                                       .ToArray();

                var propertyInfo = typeof(Client).GetProperty(Input.PropertyName);
                if (propertyInfo != null)
                {
                    propertyInfo.SetValue(this.CurrentClient, values);
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
            public string PropertyName { get; set; }
            public string PropertyValue { get; set; }
            public string[] IgnoreProperties => new string[] { "AllowedGrantTypes", "AllowedScopes" };
        }
    }
}
