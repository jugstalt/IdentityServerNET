using IdentityServer.Nova.Abstractions.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class GrantsModel : EditClientPageModel
{
    public GrantsModel(IClientDbContext clientDbContext)
         : base(clientDbContext)
    {
    }

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentClientAsync(id);

        return Page();
    }

    async public Task<IActionResult> OnGetSetAsync(string id, string grant, bool remove)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(id);

            List<string> grants = new List<string>();
            if (this.CurrentClient.AllowedGrantTypes != null)
            {
                grants.AddRange(this.CurrentClient.AllowedGrantTypes);
            }

            if (!String.IsNullOrWhiteSpace(grant))
            {
                bool hasChanged = false;
                if (remove)
                {
                    hasChanged = grants.Remove(grant);
                }
                else if (!grants.Contains(grant))
                {
                    hasChanged = true;
                    grants.Add(grant);
                }

                if (hasChanged)
                {
                    this.CurrentClient.AllowedGrantTypes = grants.ToArray();
                    await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "AllowedGrantTypes" });
                }
            }
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , "");
    }
}
