using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Servivces.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker;

public class SecretsModel : EditLockerPageModel
{
    public SecretsModel(ISecretsVaultDbContext secretsVaultDb)
        : base(secretsVaultDb)
    {
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentLockerAsync(id);

        Input = new CreateSecretInput()
        {
            LockerName = id
        };

        this.VaultSecrets = await _secretsVaultDb.GetVaultSecretsAsync(id, CancellationToken.None);

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        string secretName = String.Empty;
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentLockerAsync(Input.LockerName);

            if (!ModelState.IsValid)
            {
                throw new StatusMessageException($"Type a valid secret name.");
            }

            var secret = new VaultSecret()
            {
                Name = Input.Name,
                Description = Input.Description
            };
            await _secretsVaultDb.CreateVaultSecretAsync(Input.LockerName, secret, CancellationToken.None);

            secretName = secret.Name;
        }
        , onFinally: () => RedirectToPage("EditVaultSecret/Index", new { id = secretName, locker = Input.LockerName })
        , successMessage: ""
        , onException: (ex) => Page());
    }

    public IEnumerable<VaultSecret> VaultSecrets { get; set; }

    [BindProperty]
    public CreateSecretInput Input { get; set; }

    public class CreateSecretInput
    {
        [HiddenInput]
        public string LockerName { get; set; }

        [Required]
        [RegularExpression(@"^[a-z0-9\-_]*$", ErrorMessage = "Only lowercase letters, numbers, -, _ is allowed")]
        [MinLength(3)]
        public string Name { get; set; }

        public string Description { get; set; }
    }


}
