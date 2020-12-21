using IdentityServer.Legacy.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IClientDbContext
    {
        Task<ClientModel> FindClientByIdAsync(string clientId);
    }
}
