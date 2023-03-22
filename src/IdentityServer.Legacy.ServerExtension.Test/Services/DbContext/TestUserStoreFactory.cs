using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        async protected override Task<IUserDbContext> GetUserDbContectAsync()
        {
            return _userDbContextes.FirstOrDefault();
        }
    }
}
