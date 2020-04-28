using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker
{
    public class IndexModel : EditLockerPageModel
    {
        public IndexModel(ISecretsVaultDbContext secretsVaultDb)
            : base(secretsVaultDb)
        {

        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentLockerAsync(id);

            Input = new InputModel()
            {
                Name = CurrentLocker.Name,
                Description = CurrentLocker.Description
            };

            return Page();
        }

        async public  Task<IActionResult> OnPostAsync()
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentLockerAsync(Input.Name);

                CurrentLocker.Description = Input.Description;
                await _secretsVaultDb.UpadteLockerAsync(CurrentLocker, CancellationToken.None);
            }
            , onFinally: () => RedirectToPage(new { id = Input.Name })
            , successMessage: "The client has been updated successfully");
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [DisplayName("Name")]
            public string Name { get; set; }

            [DisplayName("Description")]
            public string Description { get; set; }
        }
    }
}
