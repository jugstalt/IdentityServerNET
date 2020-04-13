using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources.EditClient
{
    public class DeleteClientModel : EditClientPageModel
    {
        public DeleteClientModel(IClientDbContext clientDbContext)
             : base(clientDbContext)
        {
        }

        async public Task<IActionResult> OnGetAsync(string id)
        {
            await LoadCurrentClientAsync(id);

            Input = new InputModel()
            {
                ClientId = CurrentClient.ClientId
            };

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            return await PostFormHandlerAsync(async () =>
            {
                await LoadCurrentClientAsync(Input.ClientId);

                if (Input.ConfirmClientId == CurrentClient.ClientId)
                {
                    await _clientDb.RemoveClientAsync(this.CurrentClient);
                } 
                else
                {
                    throw new Exception("Please type the correct client id");
                }
            }
            , onFinally: () => RedirectToPage("../Clients")
            , "Successfully deleted client"
            , onException: (ex) => RedirectToPage(new { id = Input.ClientId }));
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            public string ClientId { get; set; }
            public string ConfirmClientId { get; set; }
        }
    }
}
