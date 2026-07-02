using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public class DomainsModel : EditRealmPageModel
{
    public DomainsModel(IRealmDbContext realmDb) : base(realmDb) { }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        public string Name { get; set; }

        [Display(Name = "Domains (one per line)")]
        public string Domains { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string name)
    {
        await LoadCurrentRealmAsync(name);
        if (CurrentRealm == null) return NotFound();

        Input = new InputModel
        {
            Name = CurrentRealm.Name,
            Domains = string.Join("\n", CurrentRealm.Domains ?? Enumerable.Empty<string>())
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentRealmAsync(Input.Name);
            if (CurrentRealm == null) throw new StatusMessageException("Realm not found.");

            CurrentRealm.Domains = ParseDomains(Input.Domains);

            try { CurrentRealm.NormalizeAndValidate(); }
            catch (ArgumentException ax) { throw new StatusMessageException(ax.Message); }

            await _realmDb.UpdateAsync(CurrentRealm, CancellationToken.None);
        },
        onFinally: () => RedirectToPage(new { name = Input.Name }),
        successMessage: "Domains updated.",
        onException: (ex) => RedirectToPage(new { name = Input.Name }));
    }

    private static List<string> ParseDomains(string raw)
        => (raw ?? "")
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => d.Trim().ToLowerInvariant())
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct()
            .ToList();
}
