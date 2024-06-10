using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public abstract class UserStoreFactory : IUserStoreFactory
    {
        private IUserDbContext _userDbContext = null;

        public UserStoreFactory()
        {
        }

        async public Task<IUserDbContext> CreateUserDbContextInstance()
        {
            if (_userDbContext != null)
            {
                return _userDbContext;
            }

            return await GetUserDbContectAsync();
        }

        abstract protected Task<IUserDbContext> GetUserDbContectAsync();
    }
}
