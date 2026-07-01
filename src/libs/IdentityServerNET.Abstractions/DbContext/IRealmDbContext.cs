using IdentityServerNET.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

/// <summary>
/// Storage for realms. A realm owns a set of user e-mail domains; a domain may belong to at most
/// one realm. This context is managed exclusively by the system administrator — realm admins never
/// touch it.
/// </summary>
public interface IRealmDbContext
{
    Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken);

    /// <summary>
    /// Finds the realm that owns the given user e-mail domain, or <c>null</c> if the domain is not
    /// assigned to any realm (i.e. belongs to the global namespace).
    /// </summary>
    Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken);

    Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken);

    Task CreateAsync(RealmModel realm, CancellationToken cancellationToken);

    Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken);

    Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken);
}
