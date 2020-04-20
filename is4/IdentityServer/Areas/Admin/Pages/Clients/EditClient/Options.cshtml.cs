using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
{
    public class OptionsModel : EditClientPageModel
    {
        public OptionsModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            return Page();
        }

        async public Task<IActionResult> OnGetSetAsync(string id, string option, bool value)
        {
            return await SecureHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(id);

                var property = this.CurrentClient.GetType().GetProperty(option);
                if (property != null)
                {
                    property.SetValue(this.CurrentClient, value);
                    await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { option });
                }
            }
            , onFinally: () => RedirectToPage(new { id = id })
            , "");
        }
    }
}
