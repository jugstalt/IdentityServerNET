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
        this.CurrentClient = await _clientDb.FindClientByIdAsync(id);
    }

    protected IClientDbContextModify _clientDb = null;
}
