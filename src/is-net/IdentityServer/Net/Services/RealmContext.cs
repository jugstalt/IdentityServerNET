#nullable enable
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

/// <summary>
/// Derives the current realm from the logged-in user's e-mail domain (see <see cref="IRealmContext"/>).
/// Resolution is cached for the lifetime of the (scoped) instance, i.e. once per request.
/// </summary>
public class RealmContext : IRealmContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRealmDbContext _realmDb;

    private bool _resolved;
    private RealmModel? _current;

    public RealmContext(IHttpContextAccessor httpContextAccessor, IRealmDbContext realmDb)
    {
        _httpContextAccessor = httpContextAccessor;
        _realmDb = realmDb;
    }

    public async Task<RealmModel?> GetCurrentRealmAsync(CancellationToken cancellationToken = default)
    {
        // A privileged provisioning/deprovisioning scope overrides the caller's realm.
        if (RealmScopeOverride.IsActive)
        {
            return RealmScopeOverride.Current is null
                ? null
                : await _realmDb.FindByNameAsync(RealmScopeOverride.Current, cancellationToken);
        }

        if (_resolved)
        {
            return _current;
        }

        _resolved = true;

        var email = GetCurrentUserEmail(_httpContextAccessor.HttpContext?.User);
        if (email is null)
        {
            return _current = null;
        }

        var domain = email.Substring(email.LastIndexOf('@') + 1).ToLowerInvariant();

        return _current = await _realmDb.FindByDomainAsync(domain, cancellationToken);
    }

    public async Task<string?> GetCurrentRealmNameAsync(CancellationToken cancellationToken = default)
    {
        // Cheap path for the override: no domain/name lookup needed.
        if (RealmScopeOverride.IsActive)
        {
            return RealmScopeOverride.Current;
        }

        return (await GetCurrentRealmAsync(cancellationToken))?.Name;
    }

    private static string? GetCurrentUserEmail(ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        // The username is always an e-mail in IdentityServer.NET. Try the usual carriers and keep the
        // first value that is actually a valid e-mail address.
        string?[] candidates =
        {
            principal.FindFirst("email")?.Value,
            principal.FindFirst(ClaimTypes.Email)?.Value,
            principal.FindFirst("preferred_username")?.Value,
            principal.FindFirst("name")?.Value,
            principal.FindFirst(ClaimTypes.Name)?.Value,
            principal.Identity?.Name,
        };

        return candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c) && c!.IsValidEmailAddress());
    }
}
