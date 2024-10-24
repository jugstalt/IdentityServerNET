using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IUserDbContext
{
    Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken);
    Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken);
    Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);
    Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken);
    Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation);
    Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation);

    UserDbContextConfiguration ContextConfiguration { get; }
}
