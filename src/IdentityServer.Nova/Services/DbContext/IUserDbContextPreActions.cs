using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface IUserDbContextPreActions : IUserDbContext
    {
        Task<ApplicationUser> PreCreateAsync(ApplicationUser user, CancellationToken cancellationToken);
    }
}
