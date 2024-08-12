using IdentityServer.Nova.Models;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.DbContext;

public interface IUserDbContextPreActions : IUserDbContext
{
    Task<ApplicationUser> PreCreateAsync(ApplicationUser user, CancellationToken cancellationToken);
}
