using IdentityModel;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditApi;

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

    async public Task<IActionResult> OnGetRemoveAsync(string id, int secretIndex, string secretHash)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApiResourceAsync(id);

            if (this.CurrentApiResource.ApiSecrets != null && secretIndex >= 0 && this.CurrentApiResource.ApiSecrets.Count() > secretIndex)
            {
                var deleteSecret = this.CurrentApiResource.ApiSecrets.ToArray()[secretIndex];
                if (deleteSecret.Value.ToSha256().StartsWith(secretHash))
                {
                    this.CurrentApiResource.ApiSecrets = this.CurrentApiResource.ApiSecrets
                                                                .Where(s => s != deleteSecret)
                                                                .ToArray();

                    await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, new[] { "ApiSecrets" });
                }
            }
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: "Successfully removed secret");
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentApiResourceAsync(Input.ApiName);

            if (!String.IsNullOrWhiteSpace(Input.Secret))
            {
                var secret = new SecretModel()
                {
                    Type = Input.SecretType,
                    Value = Input.Secret.Trim().ToSha256(),
                    Description = $"{Input.SecretDescription} (created {DateTime.Now.ToShortDateString()} {DateTime.Now.ToLongTimeString()})",
                    Expiration = Input.Expiration
                };

                List<SecretModel> clientSecrets = new List<SecretModel>();
                if (this.CurrentApiResource.ApiSecrets != null)
                {
                    clientSecrets.AddRange(this.CurrentApiResource.ApiSecrets);
                }
                clientSecrets.Add(secret);

                this.CurrentApiResource.ApiSecrets = clientSecrets.ToArray();

                await _resourceDb.UpdateApiResourceAsync(this.CurrentApiResource, new[] { "ApiSecrets" });
            }
        }
        , onFinally: () => RedirectToPage(new { id = Input.ApiName })
        , successMessage: "Secret successfully added"
        , onException: (ex) => RedirectToPage(new { id = Input.ApiName }));
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
