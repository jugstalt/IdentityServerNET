using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IdentityServer.Legacy.Extensions;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
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

            //IdentityServerConstants.SecretTypes.

            Input = new NewSecretModel()
            {
                ClientId = id,
                SecretType=IdentityServer4.IdentityServerConstants.SecretTypes.SharedSecret
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                var inputSecret = Input.Secret.Trim();

                switch(Input.SecretType)
                {
                    case IdentityServerConstants.SecretTypes.SharedSecret:
                        inputSecret = inputSecret.Sha256();
                        break;
                    case IdentityServerConstants.SecretTypes.X509CertificateBase64:
                        inputSecret = inputSecret.ParseCertBase64String();
                        break;
                    default:
                        throw new Exception("Unknown secret type");
                }

                if (!String.IsNullOrWhiteSpace(Input.Secret))
                {
                    var secret = new Secret()
                    {
                        Type = Input.SecretType,
                        Value = inputSecret,
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
                    await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "ClientSecrets" });
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.ClientId })
            , successMessage: "Secrets updated successfully");
        }

        async public Task<IActionResult> OnGetRemoveAsync(string id, int secretIndex, string secretHash)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(id);

                if (this.CurrentClient.ClientSecrets != null && secretIndex >= 0 && this.CurrentClient.ClientSecrets.Count() > secretIndex)
                {
                    var deleteSecret = this.CurrentClient.ClientSecrets.ToArray()[secretIndex];
                    if (deleteSecret.Value.ToSha256().StartsWith(secretHash))
                    {
                        this.CurrentClient.ClientSecrets = this.CurrentClient.ClientSecrets
                                                                    .Where(s => s != deleteSecret)
                                                                    .ToArray();

                        await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "ClientSecrets" });
                    }
                }
            }
            , onFinally: () => RedirectToPage(new { id = id })
            , successMessage: "Successfully removed secret");
        }

        [BindProperty]
        public NewSecretModel Input { get; set; }

        public class NewSecretModel
        {
            public string ClientId { get; set; }
           
            public string Secret { get; set; }
            public string SecretType { get; set; }
            public string SecretDescription { get; set; }
            public DateTime? Expiration { get; set; }
        }
    }
}
