using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Exceptions;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public class IndexModel : EditRealmPageModel
{
    public IndexModel(IRealmDbContext realmDb) : base(realmDb) { }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        public string Name { get; set; }

        [Display(Name = "Display name")]
        public string DisplayName { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string name)
    {
        await LoadCurrentRealmAsync(name);
        if (CurrentRealm == null) return NotFound();

        Input = new InputModel
        {
            Name = CurrentRealm.Name,
            DisplayName = CurrentRealm.DisplayName
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentRealmAsync(Input.Name);
            if (CurrentRealm == null) throw new StatusMessageException("Realm not found.");

            CurrentRealm.DisplayName = Input.DisplayName ?? "";
            await _realmDb.UpdateAsync(CurrentRealm, CancellationToken.None);
        },
        onFinally: () => RedirectToPage(new { name = Input.Name }),
        successMessage: "Realm updated.",
        onException: (ex) => RedirectToPage(new { name = Input.Name }));
    }
}
