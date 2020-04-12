using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class SecretsModel : EditClientPageModel
    {
        public SecretsModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            Input = new NewSecretModel()
            {
                ClientId = id
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                if (!String.IsNullOrWhiteSpace(Input.Secret))
                {
                    var secret = new Secret()
                    {
                        Type = Input.SecretType,
                        Value = Input.Secret.Trim().ToSha256(),
                        Description = $"{ Input.SecretDescription } (created { DateTime.Now.ToShortDateString() } { DateTime.Now.ToLongTimeString() })",
                        Expiration = Input.Expiration
                    };

                    List<Secret> clientSecrets = new List<Secret>();
                    if (this.CurrentClient.ClientSecrets != null)
                    {
                        clientSecrets.AddRange(this.CurrentClient.ClientSecrets);
                    }
                    clientSecrets.Add(secret);

                    this.CurrentClient.ClientSecrets = clientSecrets.ToArray();

                    await _clientDb.UpdateClientAsync(this.CurrentClient);
                }

                return RedirectToPage(new { id = Input.ClientId });
            }, onException: (ex) => RedirectToPage(new { id = Input.ClientId }));
        }

        [BindProperty]
        public NewSecretModel Input { get; set; }

        public class NewSecretModel
        {
            public string ClientId { get; set; }
           
            public string Secret { get; set; }
            public string SecretType => "SharedSecret";
            public string SecretDescription { get; set; }
            public DateTime? Expiration { get; set; }
        }
    }
}
