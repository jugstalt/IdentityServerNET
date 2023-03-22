using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IUserStoreFactory
    {
        Task<IUserDbContext> CreateUserDbContextInstance();
    }
}
