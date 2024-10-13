using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.DbContext;

public interface IRoleDbContext
{
    Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken);
    Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken);
    Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken);
    Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken);
    Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken);
    Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellation);
}
