using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface IUserStoreFactory
    {
        Task<IUserDbContext> CreateUserDbContextInstance();
    }
}
