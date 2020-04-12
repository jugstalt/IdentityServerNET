using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class ClientsModel : AdminPageModel
    {
        private IClientDbContextModify _clientDb = null;
        public ClientsModel(IClientDbContext clientDbContext)
        {
            _clientDb = clientDbContext as IClientDbContextModify;
        }

        async public Task<IActionResult> OnGetAsync()
        {
            if (_clientDb != null)
            {
                this.Clients = await _clientDb.GetAllClients();

                Input = new NewClient();
            }

            return Page();
        }

        async public Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                // is valid client id
                string clientId = Input.ClientId.Trim().ToLower();

                if (_clientDb != null)
                {
                    var client = new Client()
                    {
                        ClientId = clientId,
                        ClientName = Input.ClientName?.Trim()
                    };

                    await _clientDb.AddClientAsync(client);
                }

                return RedirectToPage("EditClient/Index", new { id = clientId });
            });
        }

        public IEnumerable<Client> Clients { get; set; }

        [BindProperty]
        public NewClient Input { get; set; }

        public class NewClient
        {
            public string ClientId { get; set; }
            public string ClientName { get; set; }
        }
    }
}
