using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Servivces.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.SecretsVault.EditLocker;

public class DeleteModel : EditLockerPageModel
{
    private readonly IResourceDbContext _resourceDb;

    public DeleteModel(ISecretsVaultDbContext secretsVaultDb, IResourceDbContext resourceDb = null)
        : base(secretsVaultDb)
    {
        _resourceDb = resourceDb;
    }

    [BindProperty]
    public InputModel Input { get; set; }

    public class InputModel
    {
        [HiddenInput]
        public string CurrentLockerName { get; set; }

        [Display(Name = "Confirm locker name")]
        public string ConfirmLockerName { get; set; }
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await base.LoadCurrentLockerAsync(id);
        if (this.CurrentLocker == null)
        {
            return NotFound($"Unable to load locker.");
        }

        this.Input = new InputModel()
        {
            CurrentLockerName = this.CurrentLocker.Name,
        };

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        return await base.SecureHandlerAsync(async () =>
        {
            await base.LoadCurrentLockerAsync(Input.CurrentLockerName);

            #region Verify locker name

            if (!this.CurrentLocker.Name.Equals(Input.ConfirmLockerName))
            {
                throw new StatusMessageException("Please type the correct locker name.");
            }

            #endregion

            await _secretsVaultDb.RemoveLockerAsync(Input.CurrentLockerName, CancellationToken.None);

            if (_resourceDb is not null && Input.CurrentLockerName.HasRealm())
            {
                await SecretsVaultScopeSync.RevokeLockerScopeAsync(_resourceDb, Input.CurrentLockerName);
            }
        }
        , onFinally: () => RedirectToPage("../Index")
        , successMessage: ""
        , onException: (ex) => RedirectToPage(new { id = Input.CurrentLockerName }));
    }
}
