using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Servivces.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault;

public class IndexModel : SecurePageModel
{
    private ISecretsVaultDbContext _secretsVaultDb = null;
    private IRealmContext _realmContext = null;
    private IResourceDbContext _resourceDb = null;

    public IndexModel(
        ISecretsVaultDbContext secretsVaultDbContext,
        IRealmContext realmContext = null,
        IResourceDbContext resourceDbContext = null)
    {
        _secretsVaultDb = secretsVaultDbContext;
        _realmContext = realmContext;
        _resourceDb = resourceDbContext;
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
        var realm = _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();
        var all = await _secretsVaultDb.GetLockersAsync(CancellationToken.None);

        this.Lockers = all.Where(l => l.Name.BelongsToRealm(realm)).ToArray();

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

            var realm = _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();
            var locker = new SecretsLocker()
            {
                Name = CreateLockerInput.Name.AddRealmNamespace(realm),
                Description = CreateLockerInput.Description
            };
            await _secretsVaultDb.CreateLockerAsync(locker, CancellationToken.None);

            lockerName = locker.Name;

            // Auto-grant a client-credentials scope for this locker on the shared "secrets-vault"
            // resource, if it already exists — silently skipped otherwise (browser retrieval doesn't
            // need it; the system admin can add it manually later).
            if (realm is not null && _resourceDb is not null)
            {
                await SecretsVaultScopeSync.GrantLockerScopeAsync(_resourceDb, lockerName);
            }

        },
        onFinally: () => RedirectToPage("EditLocker/Index", new { id = lockerName }),
        successMessage: "",
        onException: (ex) => Page());
    }
}
