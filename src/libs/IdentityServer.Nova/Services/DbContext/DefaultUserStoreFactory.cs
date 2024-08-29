using IdentityServer.Nova.Abstractions.DbContext;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext;
public class DefaultUserStoreFactory : UserStoreFactory
{
    private readonly IEnumerable<IUserDbContext> _userDbContextes;

    public DefaultUserStoreFactory(IEnumerable<IUserDbContext> userContextes)
    {
        _userDbContextes = userContextes;
    }

    protected override Task<IUserDbContext> GetUserDbContectAsync()
    {
        return Task.FromResult(_userDbContextes.FirstOrDefault());
    }
}
