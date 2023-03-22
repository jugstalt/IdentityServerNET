using IdentityServer.Legacy.Models.IdentityServerWrappers;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient
{
    public interface IEditClientPageModel
    {
        public ClientModel CurrentClient { get; set; }
    }
}
