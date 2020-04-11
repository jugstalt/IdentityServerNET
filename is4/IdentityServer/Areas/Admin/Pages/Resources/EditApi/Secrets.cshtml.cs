using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi
{
    public class SecretsModel : EditApiResourcePageModel
    {
        public SecretsModel(IResourceDbContext resourceDbContext)
             : base(resourceDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentApiResourceAsync(id);

            Input = new NewSecretModel()
            {
                ApiName = id
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            await LoadCurrentApiResourceAsync(Input.ApiName);

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
                if (this.CurrentApiResource.ApiSecrets != null)
                {
                    clientSecrets.AddRange(this.CurrentApiResource.ApiSecrets);
                }
                clientSecrets.Add(secret);

                this.CurrentApiResource.ApiSecrets = clientSecrets.ToArray();

                await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource);
            }

            return RedirectToPage(new { id = Input.ApiName });
        }

        [BindProperty]
        public NewSecretModel Input { get; set; }

        public class NewSecretModel
        {
            public string ApiName { get; set; }

            public string Secret { get; set; }
            public string SecretType => "SharedSecret";
            public string SecretDescription { get; set; }
            public DateTime? Expiration { get; set; }
        }
    }
}
