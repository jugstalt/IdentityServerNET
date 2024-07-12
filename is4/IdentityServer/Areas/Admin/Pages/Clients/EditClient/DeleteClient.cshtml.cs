using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

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
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(Input.ClientId);

            if (Input.ConfirmClientId == CurrentClient.ClientId)
            {
                await _clientDb.RemoveClientAsync(this.CurrentClient);
            }
            else
            {
                throw new StatusMessageException("Please type the correct client id");
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
