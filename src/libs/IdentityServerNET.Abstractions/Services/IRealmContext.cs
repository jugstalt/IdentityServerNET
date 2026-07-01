using IdentityServerNET.Models;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.Services;

/// <summary>
/// Resolves the realm of the current caller. For administrative operations the realm is derived
/// from the logged-in user's e-mail domain: a realm admin (e.g. <c>admin@foo.com</c>) resolves to
/// the realm that owns <c>foo.com</c>, while the system admin (<c>admin@is.net</c>, a domain owned
/// by no realm) resolves to <c>null</c> — the global namespace.
/// </summary>
public interface IRealmContext
{
    /// <summary>The current caller's realm, or <c>null</c> for the global (system) namespace.</summary>
    Task<RealmModel?> GetCurrentRealmAsync(CancellationToken cancellationToken = default);

    /// <summary>The current caller's realm slug, or <c>null</c> for the global (system) namespace.</summary>
    Task<string?> GetCurrentRealmNameAsync(CancellationToken cancellationToken = default);
}
