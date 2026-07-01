using IdentityServerNET.Models;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.Services;

/// <summary>
/// Realm scoping for users. Unlike clients/roles, users carry no <c>@realm</c> suffix — a user
/// belongs to a realm through its e-mail domain. Because the user store is consumed by the login
/// pipeline via capability-detecting casts, this scoping is applied by the admin UI, not by a store
/// decorator. The runtime login/registration paths are unaffected.
/// </summary>
public interface IRealmUserScope
{
    /// <summary>
    /// Keeps only the users the current caller may see: a realm admin sees users whose e-mail domain
    /// belongs to its realm; the system admin sees only users whose domain is owned by no realm.
    /// </summary>
    Task<IEnumerable<ApplicationUser>> FilterToCurrentRealmAsync(IEnumerable<ApplicationUser> users, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that the current caller may create a user with the given name (e-mail). Returns
    /// <c>null</c> when allowed, otherwise a human-readable reason for the denial.
    /// </summary>
    Task<string?> ValidateUserInCurrentRealmAsync(string userName, CancellationToken cancellationToken);
}
