using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms;

public class IndexModel : SecurePageModel
{
    private readonly IRealmDbContext _realmDb;
    private readonly IRealmProvisioningService _provisioning;

    private RealmProvisioningResult _created;

    public IndexModel(IRealmDbContext realmDb, IRealmProvisioningService provisioning)
    {
        _realmDb = realmDb;
        _provisioning = provisioning;
    }

    public IEnumerable<RealmModel> Realms { get; set; }

    [BindProperty]
    public CreateInputModel CreateInput { get; set; }

    public class CreateInputModel
    {
        [Required]
        [MinLength(3)]
        [RegularExpression(@"^[a-z0-9\-]+$", ErrorMessage = "Only lowercase letters, numbers and '-' are allowed")]
        [Display(Name = "Realm name")]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Primary domain")]
        public string PrimaryDomain { get; set; }

        [Display(Name = "Additional domains (comma or space separated)")]
        public string AdditionalDomains { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        this.Realms = await _realmDb.GetAllAsync(CancellationToken.None);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var result = await SecureHandlerAsync(async () =>
        {
            if (!ModelState.IsValid)
            {
                throw new StatusMessageException("Please provide a valid realm name and primary domain.");
            }

            var domains = new List<string> { CreateInput.PrimaryDomain };
            if (!string.IsNullOrWhiteSpace(CreateInput.AdditionalDomains))
            {
                domains.AddRange(CreateInput.AdditionalDomains
                    .Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
            }

            var realm = new RealmModel
            {
                Name = CreateInput.Name,
                PrimaryDomain = CreateInput.PrimaryDomain,
                Domains = domains
            };

            _created = await _provisioning.CreateRealmAsync(realm, CancellationToken.None);
        },
        onFinally: () =>
        {
            if (_created != null)
            {
                StatusMessage =
                    $"Realm '{_created.RealmName}' created. Admin login: {_created.AdminUserName} — " +
                    $"password: {_created.AdminPassword} (shown once, please store it now).";
            }
            return RedirectToPage();
        },
        successMessage: "",
        onException: (ex) => RedirectToPage());

        return result;
    }

    public Task<IActionResult> OnPostDeleteAsync(string name)
        => SecureHandlerAsync(async () =>
        {
            var realm = await _realmDb.FindByNameAsync(name, CancellationToken.None);
            if (realm != null)
            {
                await _realmDb.DeleteAsync(realm, CancellationToken.None);
            }
        },
        onFinally: () => RedirectToPage(),
        successMessage: $"Realm '{name}' removed. Its realm admin and realm-scoped roles are left in place " +
                        "and become inaccessible (their domain no longer maps to a realm).",
        onException: (ex) => RedirectToPage());
}
