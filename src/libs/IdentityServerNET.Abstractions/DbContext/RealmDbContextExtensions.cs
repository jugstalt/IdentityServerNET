using IdentityServerNET.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public static class RealmDbContextExtensions
{
    /// <summary>
    /// Enforces the core realm invariant: a domain may belong to at most one realm. Throws
    /// <see cref="InvalidOperationException"/> on the first domain that is already assigned to a
    /// <em>different</em> realm. Call this from every create/update path in the storage backends.
    /// </summary>
    public static async Task EnsureDomainsAvailableAsync(
        this IRealmDbContext db, RealmModel realm, CancellationToken cancellationToken)
    {
        foreach (var domain in realm.Domains)
        {
            var owner = await db.FindByDomainAsync(domain, cancellationToken);

            if (owner is not null &&
                !string.Equals(owner.Name, realm.Name, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Domain '{domain}' is already assigned to realm '{owner.Name}'.");
            }
        }
    }
}
