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

        // Cross-realm guard: a user may only obtain tokens for a client of its own realm, unless
        // the client explicitly allows the user's e-mail domain (AllowedUserDomains). Global
        // clients (no realm suffix) are usable by everyone. The user's realm is derived from its
        // e-mail domain.
        var (userRealm, userDomain) = await GetUserRealmAndDomainAsync(user);
        context.IsActive = context.Client?.ClientAllowsUser(userRealm, userDomain) ?? true;
    }

    private async Task<(string? realm, string? domain)> GetUserRealmAndDomainAsync(ApplicationUser user)
    {
        var email = string.IsNullOrEmpty(user.Email) ? user.UserName : user.Email;
        if (string.IsNullOrEmpty(email))
        {
            return (null, null);
        }

        int at = email!.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
        {
            return (null, null);
        }

        var domain = email.Substring(at + 1).ToLowerInvariant();
        var realm = (await _realmDb.FindByDomainAsync(domain, CancellationToken.None))?.Name;
        return (realm, domain);
    }
}
