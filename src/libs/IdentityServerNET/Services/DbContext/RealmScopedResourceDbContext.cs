using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

/// <summary>
/// Wraps a resource store and applies realm scoping to administrative operations while leaving the
/// lookups used by IdentityServer untouched.
///
/// <para>Read (Find*/GetAll*): pass-through with the full name — IdentityServer resolves resources
/// and scopes by their full name at runtime, and must see every realm (e.g. client_credentials has
/// no ambient user/realm).</para>
/// <para>Modify (admin): Add appends the current realm to the resource name; Update/Remove deny access
/// to a foreign realm's resource. A <c>null</c> realm keeps the resource global.</para>
///
/// <para>Note: only the resource NAME is realm-namespaced here (admin ownership). Realm-namespacing of
/// scope names — which affects tokens, consent and client AllowedScopes — is deferred to a later phase.</para>
/// </summary>
public class RealmScopedResourceDbContext : IResourceDbContext, IResourceDbContextModify
{
    private readonly IResourceDbContext _inner;
    private readonly IResourceDbContextModify _innerModify;
    private readonly IRealmContext _realmContext;

    public RealmScopedResourceDbContext(IResourceDbContext inner, IRealmContext realmContext = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _innerModify = inner as IResourceDbContextModify;
        _realmContext = realmContext;
    }

    #region IResourceDbContext (runtime — pass-through with the full name)

    public Task<ApiResourceModel> FindApiResourceAsync(string name)
        => _inner.FindApiResourceAsync(name);

    public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        => _inner.FindApiResourcesByScopeAsync(scopeNames);

    public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
        => _inner.GetAllApiResources();

    public Task<IdentityResourceModel> FindIdentityResource(string name)
        => _inner.FindIdentityResource(name);

    public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
        => _inner.GetAllIdentityResources();

    #endregion

    #region IResourceDbContextModify (admin — realm-scoped)

    public async Task AddApiResourceAsync(ApiResourceModel apiResource)
    {
        var modify = Modify();
        var realm = await CurrentRealmAsync();

        apiResource.Name = ScopeName(apiResource.Name, realm);

        await modify.AddApiResourceAsync(apiResource);
    }

    public async Task UpdateApiResourceAsync(ApiResourceModel resource, IEnumerable<string> propertyNames = null)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(resource.Name);

        await modify.UpdateApiResourceAsync(resource, propertyNames);
    }

    public async Task RemoveApiResourceAsync(ApiResourceModel apiResource)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(apiResource.Name);

        await modify.RemoveApiResourceAsync(apiResource);
    }

    public async Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        var modify = Modify();
        var realm = await CurrentRealmAsync();

        // Global reserved names (openid, profile, email, …) must not be realm-scoped.
        // If a realm admin tries to create one that already exists, explain clearly instead
        // of letting a cryptic DB duplicate-key error surface.
        var baseName = identityResource.Name.GetRealmScopedName();
        if (realm is not null && baseName.IsGlobalReservedName())
        {
            var existing = await _inner.FindIdentityResource(baseName);
            if (existing is not null)
                throw new StatusMessageException(
                    $"'{baseName}' is a standard OIDC resource and is globally available to all clients, " +
                    $"including realm-scoped ones. It does not need to be added to your realm.");
        }

        identityResource.Name = ScopeName(identityResource.Name, realm);
        await modify.AddIdentityResourceAsync(identityResource);
    }

    public async Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string> propertyNames = null)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(identityResource.Name);

        await modify.UpdateIdentityResourceAsync(identityResource, propertyNames);
    }

    public async Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(identityResource.Name);

        await modify.RemoveIdentityResourceAsync(identityResource);
    }

    #endregion

    private async Task<string> CurrentRealmAsync()
        => _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();

    // Applies the realm suffix, except for global reserved names (standard OIDC scopes such as
    // openid/profile), which must stay global even when a realm admin creates them.
    private static string ScopeName(string name, string realm)
        => name.IsGlobalReservedName() ? name.GetRealmScopedName() : name.AddRealmNamespace(realm);

    private async Task EnsureOwnedByCurrentRealmAsync(string name)
    {
        var realm = await CurrentRealmAsync();
        if (!name.BelongsToRealm(realm))
        {
            throw new UnauthorizedAccessException(
                $"Resource '{name}' does not belong to the current realm.");
        }
    }

    private IResourceDbContextModify Modify()
        => _innerModify ?? throw new NotSupportedException(
            "The underlying resource store does not support modifications.");
}
