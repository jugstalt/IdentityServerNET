#nullable enable
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class ProfileService : IProfileService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRealmDbContext _realmDb;

    public ProfileService(UserManager<ApplicationUser> userManager, IRealmDbContext realmDb)
    {
        _userManager = userManager;
        _realmDb = realmDb;
    }

    public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        return Task.FromResult(0);
    }

    async public Task IsActiveAsync(IsActiveContext context)
    {
        var user = await _userManager.GetUserAsync(context.Subject);
        if (user is null)
        {
            context.IsActive = false;
            return;
        }

        // Cross-realm guard: a user may only obtain tokens for a client of its own realm. Global
        // clients (no realm suffix) are usable by everyone. The user's realm is derived from its
        // e-mail domain.
        var userRealm = await GetUserRealmAsync(user);
        context.IsActive = context.Client?.ClientId.ClientAllowsUserRealm(userRealm) ?? true;
    }

    private async Task<string?> GetUserRealmAsync(ApplicationUser user)
    {
        var email = string.IsNullOrEmpty(user.Email) ? user.UserName : user.Email;
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        int at = email!.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
        {
            return null;
        }

        var domain = email.Substring(at + 1).ToLowerInvariant();
        return (await _realmDb.FindByDomainAsync(domain, CancellationToken.None))?.Name;
    }
}
