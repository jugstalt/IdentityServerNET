using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.DbContext;

public interface IUserStoreFactory
{
    Task<IUserDbContext> CreateUserDbContextInstance();
}
