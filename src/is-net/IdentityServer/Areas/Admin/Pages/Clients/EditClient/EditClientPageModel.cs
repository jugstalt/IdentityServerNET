using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class EditClientPageModel : AdminPageModel, IEditClientPageModel
{
    public EditClientPageModel(IClientDbContext clientDbContext)
    {
        _clientDb = clientDbContext as IClientDbContextModify;
    }

    #region IEditClientModel

    public ClientModel CurrentClient { get; set; }

    #endregion

    async public Task LoadCurrentClientAsync(string id)
    {
        var client = await _clientDb.FindClientByIdAsync(id);
        this.CurrentClient = client != null && await this.IsInCurrentRealmAsync(client.ClientId)
            ? client
            : null;
    }

    protected IClientDbContextModify _clientDb = null;
}
