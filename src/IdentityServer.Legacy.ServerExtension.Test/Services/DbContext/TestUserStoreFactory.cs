using IdentityServer.Legacy.Services.DbContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.ServerExtension.Test.Services.DbContext
{
    internal class TestUserStoreFactory : UserStoreFactory
    {
        private readonly IEnumerable<IUserDbContext> _userDbContextes;

        public TestUserStoreFactory(IEnumerable<IUserDbContext> userContextes)
        {
            _userDbContextes = userContextes;
        }

        protected override Task<IUserDbContext> GetUserDbContectAsync()
        {
            return Task.FromResult(_userDbContextes.FirstOrDefault());
        }
    }
}
