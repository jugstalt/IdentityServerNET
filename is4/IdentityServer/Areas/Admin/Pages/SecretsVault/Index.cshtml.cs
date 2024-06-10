using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault
{
    public class IndexModel : SecurePageModel
    {
        private ISecretsVaultDbContext _secretsVaultDb = null;

        public IndexModel(ISecretsVaultDbContext secretsVaultDbContext)
        {
            _secretsVaultDb = secretsVaultDbContext;
        }

        public IEnumerable<SecretsLocker> Lockers { get; set; }

        public FindInputModel FindInput { get; set; }

        public class FindInputModel
        {
            public string Name { get; set; }
        }

        [BindProperty]
        public CreateLockerInputModel CreateLockerInput { get; set; }

        public class CreateLockerInputModel
        {
            [Required]
            [RegularExpression(@"^[a-z0-9\-_]*$", ErrorMessage = "Only lowercase letters, numbers, -, _ is allowed")]
            [MinLength(3)]
            public string Name { get; set; }
            public string Description { get; set; }
        }

        async public Task<IActionResult> OnGetAsync()
        {

            this.Lockers = await _secretsVaultDb.GetLockersAsync(CancellationToken.None);

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            string lockerName = String.Empty;

            return await SecureHandlerAsync(async () =>
            {
                if (!ModelState.IsValid)
                {
                    throw new StatusMessageException($"Type a valid locker name.");
                }

                var locker = new SecretsLocker()
                {
                    Name = CreateLockerInput.Name,
                    Description = CreateLockerInput.Description
                };
                await _secretsVaultDb.CreateLockerAsync(locker, CancellationToken.None);

                lockerName = locker.Name;

            },
            onFinally: () => RedirectToPage("EditLocker/Index", new { id = lockerName }),
            successMessage: "",
            onException: (ex) => Page());
        }
    }
}
