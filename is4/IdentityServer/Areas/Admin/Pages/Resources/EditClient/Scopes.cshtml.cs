using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class ScopesModel : EditClientPageModel
    {
        public ScopesModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            this.Input = new NewScopeModel()
            {
                ClientId = CurrentClient.ClientId
            };

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            await LoadCurrentClientAsync(Input.ClientId);

            if(!String.IsNullOrWhiteSpace(Input.ScopeName))
            {
                List<string> allowedScopes = new List<string>();
                if(this.CurrentClient.AllowedScopes!=null)
                {
                    allowedScopes.AddRange(this.CurrentClient.AllowedScopes);
                }

                if(!allowedScopes.Contains(Input.ScopeName.ToLower()))
                {
                    allowedScopes.Add(Input.ScopeName.ToLower());
                    this.CurrentClient.AllowedScopes = allowedScopes.ToArray();

                    await _clientDb.UpdateClientAsync(this.CurrentClient);
                    //await SetCurrentClient(Input.ClientId); // Reload
                }
            } 

            return RedirectToPage(new { id = Input.ClientId });
        }

        [BindProperty]
        public NewScopeModel Input { get; set; }

        public class NewScopeModel
        {
            public string ClientId { get; set; }
            public string ScopeName { get; set; }
        }
    }
}
