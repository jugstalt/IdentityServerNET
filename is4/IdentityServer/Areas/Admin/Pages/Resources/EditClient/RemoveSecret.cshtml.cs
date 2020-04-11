using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityModel;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class RemoveSecretModel : EditClientPageModel
    {
        public RemoveSecretModel(IClientDbContext clientDbContext)
            : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, int secretIndex, string secretHash)
        {
            await LoadCurrentClientAsync(id);

            if (this.CurrentClient.ClientSecrets != null && secretIndex >= 0 && this.CurrentClient.ClientSecrets.Count() > secretIndex)
            {
                var deleteSecret = this.CurrentClient.ClientSecrets.ToArray()[secretIndex];
                if(deleteSecret.Value.ToSha256().StartsWith(secretHash))
                {
                    this.CurrentClient.ClientSecrets = this.CurrentClient.ClientSecrets
                                                                .Where(s => s != deleteSecret)
                                                                .ToArray();

                    await _clientDb.UpdateClientAsync(this.CurrentClient);
                }
            }

            return RedirectToPage("Secrets", new { id = id });

        }
    }
}
