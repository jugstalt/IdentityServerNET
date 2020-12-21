using IdentityServer.Legacy.Models.IdentityServerWrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IClientDbContextModify : IClientDbContext
    {
        Task AddClientAsync(ClientModel client);
        Task UpdateClientAsync(ClientModel client, IEnumerable<string> propertyNames = null);
        Task RemoveClientAsync(ClientModel client);

        Task<IEnumerable<ClientModel>> GetAllClients();
    }
}
