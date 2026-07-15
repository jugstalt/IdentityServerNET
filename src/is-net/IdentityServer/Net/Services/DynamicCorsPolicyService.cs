using IdentityServer4.Services;
using IdentityServerNET.Abstractions.DbContext;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

// IdentityServer4's default ICorsPolicyService (DefaultCorsPolicyService) only consults its own
// AllowedOrigins/AllowAll properties, which nothing in this app ever populates - so a client's
// AllowedCorsOrigins, editable in the admin UI, was silently never enforced (CORS preflight/actual
// requests were always denied, since AllowedOrigins stays empty). This queries the real client store
// instead, the same way IdentityServer4's own InMemoryCorsPolicyService does for its static client
// list, just resolved dynamically via IClientDbContext.
public class DynamicCorsPolicyService : ICorsPolicyService
{
    private readonly IClientDbContext _clientDb;
    private readonly ILogger<DynamicCorsPolicyService> _logger;

    public DynamicCorsPolicyService(IClientDbContext clientDb, ILogger<DynamicCorsPolicyService> logger)
    {
        _clientDb = clientDb;
        _logger = logger;
    }

    public async Task<bool> IsOriginAllowedAsync(string origin)
    {
        if (_clientDb is not IClientDbContextModify clientDb)
        {
            _logger.LogDebug("Client store does not support listing clients - origin {Origin} denied.", origin);
            return false;
        }

        var clients = await clientDb.GetAllClients();

        var isAllowed = clients
            .Where(c => c.AllowedCorsOrigins != null)
            .SelectMany(c => c.AllowedCorsOrigins)
            .Select(GetOrigin)
            .Contains(origin, StringComparer.OrdinalIgnoreCase);

        _logger.LogDebug("Client list checked and origin {Origin} is {Allowed}", origin, isAllowed ? "allowed" : "not allowed");

        return isAllowed;
    }

    // IdentityServer4's own StringExtensions.GetOrigin() (used by InMemoryCorsPolicyService for the
    // exact same normalization) is internal to that assembly, so it's replicated here: a configured
    // AllowedCorsOrigins entry may include a path/query that must be stripped down to scheme+authority
    // before comparing against the browser-supplied Origin header.
    private static string GetOrigin(string url)
    {
        if (url is null || !Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        return uri.Scheme is "http" or "https" ? $"{uri.Scheme}://{uri.Authority}" : null;
    }
}
