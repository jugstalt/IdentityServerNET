using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class IndexModel : EditClientPageModel
    {
        public IndexModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            Input = new InputModel()
            {
                ClientId = CurrentClient.ClientId,
                ClientName = CurrentClient.ClientName,
                ClientDescription = CurrentClient.Description
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCurrentClientAsync(Input.ClientId);

            CurrentClient.ClientName = Input.ClientName;
            CurrentClient.Description = Input.ClientDescription;

            await _clientDb.UpdateClientAsync(CurrentClient);

            return Page();
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ClientId { get; set; }
            public string ClientName { get; set; }
            public string ClientDescription { get; set; }
        } 
    }
}
