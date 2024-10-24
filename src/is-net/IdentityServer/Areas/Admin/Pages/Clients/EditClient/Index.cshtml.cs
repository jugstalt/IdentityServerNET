using IdentityServerNET.Abstractions.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

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
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(Input.ClientId);

            CurrentClient.ClientName = Input.ClientName;
            CurrentClient.Description = Input.ClientDescription;

            await _clientDb.UpdateClientAsync(CurrentClient, new[] { "ClientName", "Description" });
        }
        , onFinally: () => RedirectToPage(new { id = Input.ClientId })
        , successMessage: "The client has been updated successfully");
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
