using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IAdminRoleDbContext
    {
        Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken);
    }
}
