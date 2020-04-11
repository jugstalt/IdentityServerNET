using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IClientDbContextModify : IClientDbContext
    {
        Task AddClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task RemoveClientAsync(Client client);

        Task<IEnumerable<Client>> GetAllClients();
    }
}
