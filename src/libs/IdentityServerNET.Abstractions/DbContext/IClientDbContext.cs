using IdentityServerNET.Models.IdentityServerWrappers;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IClientDbContext
{
    Task<ClientModel?> FindClientByIdAsync(string clientId);
}
