using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IUserRoleDbContext
    {
        Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken);
        Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken);
        Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken);
    }
}
