using Microsoft.AspNetCore.Authorization;

namespace IdentityServerNET.Authorization;

/// <summary>
/// Requires that the caller holds at least one of the given administrator roles for its current
/// realm. The realm binding (global vs. a specific realm) is resolved at evaluation time from the
/// caller's e-mail domain — see <see cref="RealmAdminAuthorizationHandler"/>.
/// </summary>
public class RealmAdminRequirement : IAuthorizationRequirement
{
    public string[] AdminRoles { get; }

    public RealmAdminRequirement(params string[] adminRoles)
    {
        AdminRoles = adminRoles ?? new string[0];
    }
}
