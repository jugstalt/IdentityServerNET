#nullable enable
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;

namespace IdentityServerNET.Authorization;

/// <summary>
/// Grants an admin capability when the caller holds the required role for the CURRENT realm: the
/// global role for the system admin (realm == null), or <c>role@realm</c> for a realm admin. The
/// realm is derived from the caller's e-mail domain (<see cref="IRealmContext"/>), the same source
/// that scopes the data.
///
/// Fail-closed: if the caller's realm cannot be resolved (e.g. its domain was removed from the
/// realm), a realm-scoped role no longer matches the required <c>role@realm</c>, so access is denied
/// rather than silently widening to the global namespace.
/// </summary>
public class RealmAdminAuthorizationHandler : AuthorizationHandler<RealmAdminRequirement>
{
    private readonly IRealmContext _realmContext;

    public RealmAdminAuthorizationHandler(IRealmContext realmContext)
    {
        _realmContext = realmContext;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, RealmAdminRequirement requirement)
    {
        var realm = await _realmContext.GetCurrentRealmNameAsync();

        foreach (var role in requirement.AdminRoles)
        {
            // Global role name when realm is null, otherwise role@realm.
            var required = role.AddRealmNamespace(realm);

            if (context.User.IsInRole(required))
            {
                context.Succeed(requirement);
                return;
            }
        }
    }
}
