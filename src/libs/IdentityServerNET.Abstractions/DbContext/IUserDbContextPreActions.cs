using IdentityServerNET.Models;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IUserDbContextPreActions : IUserDbContext
{
    Task<ApplicationUser> PreCreateAsync(ApplicationUser user, CancellationToken cancellationToken);
}
