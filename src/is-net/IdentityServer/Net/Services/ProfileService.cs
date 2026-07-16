#nullable enable
using Duende.IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

    // Claim sets per IdentityServerConstants.StandardScopes: profile, email, phone, address, role.
    async public Task GetProfileDataAsync(ProfileDataRequestContext context)
    {
        var user = await _userManager.GetUserAsync(context.Subject);
        if (user is null)
        {
            return;
        }

        var claims = new List<Claim>
        {
            new Claim(JwtClaimTypes.Name, user.ApplicationUserName()),
            // shorthand display handle, distinct from the full name - Keycloak et al. default this to the login username
            new Claim(JwtClaimTypes.PreferredUserName,
                user.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.PreferredUserName)?.Value ?? user.UserName ?? "")
        };

        // profile
        claims.AddRange(user.Claims.Where(c => c.Type is
            JwtClaimTypes.GivenName or JwtClaimTypes.FamilyName or JwtClaimTypes.MiddleName or
            JwtClaimTypes.NickName or JwtClaimTypes.Profile or
            JwtClaimTypes.Picture or JwtClaimTypes.WebSite or JwtClaimTypes.Gender or
            JwtClaimTypes.BirthDate or JwtClaimTypes.ZoneInfo or JwtClaimTypes.Locale or
            JwtClaimTypes.UpdatedAt));

        // email
        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(JwtClaimTypes.Email, user.Email));
            claims.Add(new Claim(JwtClaimTypes.EmailVerified, user.EmailConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        // address
        claims.AddRange(user.Claims.Where(c => c.Type == JwtClaimTypes.Address));

        // phone
        if (!string.IsNullOrEmpty(user.PhoneNumber))
        {
            claims.Add(new Claim(JwtClaimTypes.PhoneNumber, user.PhoneNumber));
            claims.Add(new Claim(JwtClaimTypes.PhoneNumberVerified, user.PhoneNumberConfirmed ? "true" : "false", ClaimValueTypes.Boolean));
        }

        // role - realm suffix stripped, clients only see the plain role name
        if (user.Roles is not null)
        {
            claims.AddRange(user.Roles.Select(r => new Claim(JwtClaimTypes.Role, r.GetRealmScopedName())));
        }

        context.AddRequestedClaims(claims);
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
