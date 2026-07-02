using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models;
using IdentityServerNET.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public class DeleteRealmModel : EditRealmPageModel
{
    private readonly IRealmProvisioningService _provisioning;

    public DeleteRealmModel(IRealmDbContext realmDb, IRealmProvisioningService provisioning) : base(realmDb)
    {
        _provisioning = provisioning;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [HiddenInput]
        public string Name { get; set; }

        [Required]
        [Display(Name = "Confirm realm name")]
        public string ConfirmName { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(string name)
    {
        await LoadCurrentRealmAsync(name);
        if (CurrentRealm == null) return NotFound();

        Input = new InputModel { Name = CurrentRealm.Name };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentRealmAsync(Input.Name);
            if (CurrentRealm == null) throw new StatusMessageException("Realm not found.");

            if (!string.Equals(CurrentRealm.Name, Input.ConfirmName, System.StringComparison.Ordinal))
            {
                throw new StatusMessageException(
                    $"Please type the exact realm name '{CurrentRealm.Name}' to confirm deletion.");
            }

            await _provisioning.DeleteRealmAsync(new RealmModel { Name = Input.Name }, CancellationToken.None);
        },
        onFinally: () => RedirectToPage("../Index"),
        successMessage: $"Realm '{Input.Name}' removed together with its clients, roles, resources and realm admin.",
        onException: (ex) => RedirectToPage(new { name = Input.Name }));
    }
}
