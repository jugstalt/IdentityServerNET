using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret
{
    public class DeleteModel : EditVaultSecretPageModel
    {
        public DeleteModel(ISecretsVaultDbContext secretsVaultDb)
            : base(secretsVaultDb)
        {

        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [HiddenInput]
            public string LockerName { get; set; }

            [HiddenInput]
            public string CurrentSecretName { get; set; }

            [Display(Name = "Confirm secret name")]
            public string ConfirmSecretName { get; set; }
        }

        async public Task<IActionResult> OnGetAsync(string id, string locker)
        {
            await base.LoadCurrentSecretAsync(locker, id);
            if (this.CurrentSecret == null)
            {
                return NotFound($"Unable to load locker.");
            }

            this.Input = new InputModel()
            {
                LockerName = locker,
                CurrentSecretName = this.CurrentSecret.Name,
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await base.SecureHandlerAsync(async () =>
            {
                await base.LoadCurrentSecretAsync(Input.LockerName, Input.CurrentSecretName);

                #region Verify locker name

                if (!this.CurrentSecret.Name.Equals(Input.ConfirmSecretName))
                {
                    throw new StatusMessageException("Please type the correct secret name.");
                }

                #endregion

                await _secretsVaultDb.RemoveVaultSecretAsync(Input.LockerName, Input.CurrentSecretName, CancellationToken.None);
            }
            , onFinally: () => RedirectToPage("../Secrets", new { id = Input.LockerName })
            , successMessage: ""
            , onException: (ex) => RedirectToPage(new { id = Input.CurrentSecretName, locker = Input.LockerName }));
        }
    }
}
