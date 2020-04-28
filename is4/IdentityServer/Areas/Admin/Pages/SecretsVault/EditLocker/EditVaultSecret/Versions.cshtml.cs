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
using IdentityServer.Legacy.Models;
using System.Threading;
using IdentityServer.Legacy.Exceptions;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret
{
    public class VersionsModel : EditVaultSecretPageModel
    {
        public VersionsModel(ISecretsVaultDbContext secretsVaultDb)
            : base(secretsVaultDb)
        {

        }

        async public Task<IActionResult> OnGetAsync(string id, string locker)
        {
            await LoadCurrentSecretAsync(locker, id);

            Input = new CreateSecretVersionModel()
            {
                LockerName = locker,
                SecretName = id
            };

            this.VaultSecretVersions = await _secretsVaultDb.GetVaultSecretVersionsAsync(locker, id, CancellationToken.None);

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentSecretAsync(Input.LockerName, Input.SecretName);

                var inputSecret = Input.Secret.Trim();

                if (!String.IsNullOrWhiteSpace(Input.Secret))
                {
                    var secretVersion = new VaultSecretVersion()
                    {
                        VersionTimeStamp = DateTime.UtcNow.Ticks,
                        Secret = Input.Secret  // ToDo: Encrypt!!!!
                    };

                    await _secretsVaultDb.CreateVaultSecretVersionAsync(this.LockerName, this.CurrentSecret.Name, secretVersion, CancellationToken.None);
                }
            }
            , onFinally: () => RedirectToPage(new { id = Input.SecretName, locker=Input.LockerName })
            , successMessage: "Secret version created successfully");
        }

        async public Task<IActionResult> OnGetRemoveAsync(string id, string locker, long stamp)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentSecretAsync(locker, id);

                if (CurrentSecret == null)
                {
                    throw new StatusMessageException("unknown secret");
                }

                await _secretsVaultDb.RemoveVaultSecretVersionAsync(this.LockerName, this.CurrentSecret.Name, stamp, CancellationToken.None);
            }
            , onFinally: () => RedirectToPage(new { id = id, locker = locker })
            , successMessage: "Successfully removed secret");
        }

        public IEnumerable<VaultSecretVersion> VaultSecretVersions { get; set; }

        [BindProperty]
        public CreateSecretVersionModel Input { get; set; }

        public class CreateSecretVersionModel
        {
            public string LockerName { get; set; }
            public string SecretName { get; set; }
           
            public string Secret { get; set; }
        }
    }
}
