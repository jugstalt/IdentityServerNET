using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker.EditVaultSecret
{
    public class IndexModel : EditVaultSecretPageModel
    {
        public IndexModel(ISecretsVaultDbContext secretsVaultDb)
            : base(secretsVaultDb)
        {

        }

        async public Task<IActionResult> OnGetAsync(string id, string locker)
        {
            await LoadCurrentSecretAsync(locker, id);

            Input = new InputModel()
            {
                LockerName=locker,
                Name = CurrentSecret.Name,
                Description = CurrentSecret.Description
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentSecretAsync(Input.LockerName, Input.Name);

                CurrentSecret.Description = Input.Description;
                await _secretsVaultDb.UpadteVaultSecretAsync(Input.LockerName, CurrentSecret, CancellationToken.None);
            }
            , onFinally: () => RedirectToPage(new { id = Input.Name, locker=Input.LockerName })
            , successMessage: "The client has been updated successfully");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [HiddenInput]
            public string LockerName { get; set; }

            [DisplayName("Name")]
            public string Name { get; set; }

            [DisplayName("Description")]
            public string Description { get; set; }
        }
    }
}
