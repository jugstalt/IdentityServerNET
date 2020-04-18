using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string clientId = Input.ClientId.Trim().ToLower();

            return await SecureHandlerAsync(async () =>
            {
                if (_clientDb != null)
                {
                    var client = new Client()
                    {
                        ClientId = clientId,
                        ClientName = Input.ClientName?.Trim()
                    };

                    await _clientDb.AddClientAsync(client);
                }
            }
            , onFinally: ()=> RedirectToPage("EditClient/Index", new { id = clientId })
            , successMessage: "Client successfully created"
            , onException: (ex) => RedirectToPage());
        }

        public IEnumerable<Client> Clients { get; set; }

        [BindProperty]
        public NewClient Input { get; set; }

        public class NewClient
        {
            [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
            public string ClientId { get; set; }
            public string ClientName { get; set; }
        }
    }
}
