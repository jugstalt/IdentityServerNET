using IdentityServer.Nova.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface IClientDbContext
    {
        Task<ClientModel> FindClientByIdAsync(string clientId);
    }
}
