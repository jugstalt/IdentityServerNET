using IdentityServerNET.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IAdminUserDbContext : IUserDbContext
{
    Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken);

    Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken);
}
