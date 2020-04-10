using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class SetGrantModel : EditClientPageModel
    {
        public SetGrantModel(IClientDbContext clientDbContext)
            : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id, string grant, bool remove)
        {
            await LoadCurrentClientAsync(id);

            List<string> grants = new List<string>();
            if(this.CurrentClient.AllowedGrantTypes!=null)
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

                if(hasChanged)
                {
                    this.CurrentClient.AllowedGrantTypes = grants.ToArray();
                    await _clientDb.UpdateClientAsync(this.CurrentClient);
                }
            }

            return RedirectToPage("Grants", new { id = id });

        }
    }
}
