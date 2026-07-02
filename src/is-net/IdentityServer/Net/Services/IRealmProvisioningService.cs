using IdentityServerNET.Models;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public interface IRealmProvisioningService
{
    /// <summary>
    /// Creates a realm and provisions its administration: the realm-scoped admin roles and a realm
    /// admin user <c>admin@{primary-domain}</c> with a generated password. Runs from the system-admin
    /// context. Throws when the realm/domain/admin already exists or the realm is invalid.
    /// </summary>
    Task<RealmProvisioningResult> CreateRealmAsync(RealmModel realm, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a realm and deprovisions everything it owns: its realm-scoped clients, roles and
    /// resources, plus the realm admin user, then the realm record itself. End-user accounts of the
    /// realm's domains are retained. Runs from the system-admin context via a realm override.
    /// </summary>
    Task DeleteRealmAsync(RealmModel realm, CancellationToken cancellationToken);
}

public class RealmProvisioningResult
{
    public string RealmName { get; set; } = "";
    public string AdminUserName { get; set; } = "";

    /// <summary>The generated realm-admin password — shown to the system admin once, never stored.</summary>
    public string AdminPassword { get; set; } = "";
}
