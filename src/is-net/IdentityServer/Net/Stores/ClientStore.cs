using IdentityServer4.Models;
using IdentityServer4.Stores;
using IdentityServerNET.Abstractions.DbContext;
using System.Threading.Tasks;

namespace IdentityServerNET;

class ClientStore : IClientStore
{
    public ClientStore(IClientDbContext clientDbContext)
    {
        _clientDbContext = clientDbContext;
    }

    private IClientDbContext _clientDbContext = null;

    async public Task<Client> FindClientByIdAsync(string clientId)
    {
        return (await _clientDbContext.FindClientByIdAsync(clientId))?.IdentityServer4Instance;
    }
}
