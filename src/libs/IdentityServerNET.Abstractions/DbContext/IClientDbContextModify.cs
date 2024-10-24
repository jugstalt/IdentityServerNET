using IdentityServerNET.Models.IdentityServerWrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IClientDbContextModify : IClientDbContext
{
    Task AddClientAsync(ClientModel client);
    Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null);
    Task RemoveClientAsync(ClientModel client);

    Task<IEnumerable<ClientModel>> GetAllClients();
}
