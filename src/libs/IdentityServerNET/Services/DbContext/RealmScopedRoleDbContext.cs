using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

/// <summary>
/// Wraps a role store and applies realm scoping to administrative operations while leaving lookups
/// (used to resolve a role by its full name/id) untouched.
///
/// <para>Read (FindById/FindByName): pass-through with the full name.</para>
/// <para>Create: appends the current realm to the role name/id (the admin supplies only the local
/// name). A <c>null</c> realm — system admin or startup seeding — keeps the role global, so the
/// built-in administrator roles stay in the global namespace.</para>
/// <para>Delete/Update and the admin listing are scoped to the current realm; a realm admin sees and
/// touches only its own roles, the system admin only global ones.</para>
/// </summary>
public class RealmScopedRoleDbContext : IRoleDbContext, IAdminRoleDbContext
{
    private readonly IRoleDbContext _inner;
    private readonly IAdminRoleDbContext _innerAdmin;
    private readonly IRealmContext _realmContext;

    public RealmScopedRoleDbContext(IRoleDbContext inner, IRealmContext realmContext = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _innerAdmin = inner as IAdminRoleDbContext;
        _realmContext = realmContext;
    }

    #region IRoleDbContext

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var realm = await CurrentRealmAsync();

        if (realm != null)
        {
            // Realm admin: force the role into the caller's realm.
            role.Name = role.Name.AddRealmNamespace(realm);
            if (!string.IsNullOrEmpty(role.Id))
            {
                role.Id = role.Id.AddRealmNamespace(realm);
            }
        }
        // System context (realm == null): keep the name as given. Global roles stay global, and realm
        // provisioning can create explicit role@realm names for a new realm from the system context.

        return await _inner.CreateAsync(role, cancellationToken);
    }

    public async Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var denied = await EnsureOwnedByCurrentRealmAsync(role);
        if (denied != null)
        {
            return denied;
        }

        return await _inner.DeleteAsync(role, cancellationToken);
    }

    public Task<ApplicationRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        => _inner.FindByIdAsync(roleId, cancellationToken);

    public Task<ApplicationRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        => _inner.FindByNameAsync(normalizedRoleName, cancellationToken);

    public async Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var denied = await EnsureOwnedByCurrentRealmAsync(role);
        if (denied != null)
        {
            return denied;
        }

        return await _inner.UpdateAsync(role, cancellationToken);
    }

    // Pass-through: used by the ASP.NET Identity role store internals (e.g. normalized name).
    public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellationToken)
        => _inner.UpdatePropertyAsync<T>(role, applicationRoleProperty, propertyValue, cancellationToken);

    #endregion

    #region IAdminRoleDbContext

    public async Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
    {
        var admin = Admin();
        var realm = await CurrentRealmAsync();

        // Filter to the current realm before paging, otherwise skip/limit would count foreign roles.
        return (await admin.GetRolesAsync(int.MaxValue, 0, cancellationToken))
            .Where(r => RoleIdentifier(r).BelongsToRealm(realm))
            .Skip(skip)
            .Take(limit)
            .ToArray();
    }

    public async Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
    {
        var admin = Admin();
        var realm = await CurrentRealmAsync();

        return (await admin.FindRoles(term, cancellationToken))
            .Where(r => RoleIdentifier(r).BelongsToRealm(realm))
            .ToArray();
    }

    #endregion

    private async Task<string> CurrentRealmAsync()
        => _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();

    private async Task<IdentityResult> EnsureOwnedByCurrentRealmAsync(ApplicationRole role)
    {
        var realm = await CurrentRealmAsync();
        if (!RoleIdentifier(role).BelongsToRealm(realm))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Code = "realm_forbidden",
                Description = $"Role '{RoleIdentifier(role)}' does not belong to the current realm."
            });
        }

        return null;
    }

    // Name and Id carry the same value (the admin sets both to the role name); prefer whichever is set.
    private static string RoleIdentifier(ApplicationRole role)
        => string.IsNullOrEmpty(role.Name) ? role.Id : role.Name;

    private IAdminRoleDbContext Admin()
        => _innerAdmin ?? throw new NotSupportedException(
            "The underlying role store does not support administration.");
}
