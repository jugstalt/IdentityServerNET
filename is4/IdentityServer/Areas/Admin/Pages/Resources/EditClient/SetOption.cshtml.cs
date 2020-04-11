using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class SetOptionModel : EditClientPageModel
    {
        public SetOptionModel(IClientDbContext clientDbContext)
            : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string option, bool value)
        {
            await LoadCurrentClientAsync(id);

            var property = this.CurrentClient.GetType().GetProperty(option);
            if(property!=null)
            {
                property.SetValue(this.CurrentClient, value);
                await _clientDb.UpdateClientAsync(this.CurrentClient);
            }

            return RedirectToPage("Options", new { id = id });
        }
    }
}
