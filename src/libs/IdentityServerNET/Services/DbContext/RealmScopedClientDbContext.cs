using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

/// <summary>
/// Wraps a client store and applies realm scoping to administrative operations, while leaving the
/// runtime lookup used by IdentityServer untouched.
///
/// <para>Read (<see cref="FindClientByIdAsync"/>): pass-through with the full id — the OIDC pipeline
/// sends the complete client_id (e.g. <c>my-client@xyz</c>).</para>
/// <para>Modify (Add/Update/Remove/GetAll): scoped to the current realm. The realm is never typed by
/// the admin; it is derived from who they are (<see cref="IRealmContext"/>) and appended to the id.
/// A <c>null</c> realm (system admin / multi-tenancy off) operates on the global namespace: ids
/// without a realm suffix.</para>
/// </summary>
public class RealmScopedClientDbContext : IClientDbContext, IClientDbContextModify
{
    private readonly IClientDbContext _inner;
    private readonly IClientDbContextModify _innerModify;
    private readonly IRealmContext _realmContext;

    public RealmScopedClientDbContext(IClientDbContext inner, IRealmContext realmContext = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _innerModify = inner as IClientDbContextModify;
        _realmContext = realmContext;
    }

    #region IClientDbContext (runtime — pass-through with the full id)

    public Task<ClientModel> FindClientByIdAsync(string clientId)
        => _inner.FindClientByIdAsync(clientId);

    #endregion

    #region IClientDbContextModify (admin — realm-scoped)

    public async Task AddClientAsync(ClientModel client)
    {
        var modify = Modify();
        var realm = await CurrentRealmAsync();

        // The admin supplies the local name; the realm is implied by who they are.
        client.ClientId = client.ClientId.AddRealmNamespace(realm);

        await modify.AddClientAsync(client);
    }

    public async Task<IEnumerable<ClientModel>> GetAllClients()
    {
        var modify = Modify();
        var realm = await CurrentRealmAsync();

        var all = await modify.GetAllClients();

        return all.Where(c => c.ClientId.BelongsToRealm(realm)).ToArray();
    }

    public async Task UpdateClientAsync(ClientModel client, IEnumerable<string> propertyNames = null)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(client.ClientId);

        await modify.UpdateClientAsync(client, propertyNames);
    }

    public async Task RemoveClientAsync(ClientModel client)
    {
        var modify = Modify();
        await EnsureOwnedByCurrentRealmAsync(client.ClientId);

        await modify.RemoveClientAsync(client);
    }

    #endregion

    private async Task<string> CurrentRealmAsync()
        => _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();

    private async Task EnsureOwnedByCurrentRealmAsync(string clientId)
    {
        var realm = await CurrentRealmAsync();
        if (!clientId.BelongsToRealm(realm))
        {
            throw new UnauthorizedAccessException(
                $"Client '{clientId}' does not belong to the current realm.");
        }
    }

    private IClientDbContextModify Modify()
        => _innerModify ?? throw new NotSupportedException(
            "The underlying client store does not support modifications.");
}
