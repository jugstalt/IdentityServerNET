using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IUserStoreFactory
{
    Task<IUserDbContext> CreateUserDbContextInstance();
}
