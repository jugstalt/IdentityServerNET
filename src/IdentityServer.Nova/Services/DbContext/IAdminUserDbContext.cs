using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface IAdminUserDbContext : IUserDbContext
    {
        Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken);
    }
}
