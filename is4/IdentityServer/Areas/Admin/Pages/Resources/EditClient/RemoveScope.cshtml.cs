using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class RemoveScopeModel : EditClientPageModel
    {
        public RemoveScopeModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string scopeName)
        {
            await LoadCurrentClientAsync(id);

            this.CurrentClient.AllowedScopes = this.CurrentClient
                                                    .AllowedScopes
                                                    .Where(s => s != scopeName)
                                                    .ToArray();

            await _clientDb.UpdateClientAsync(this.CurrentClient);

            return RedirectToPage("Scopes", new { id = id });

        }
    }
}
