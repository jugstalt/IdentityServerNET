using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.DbContext;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Pins the realm isolation behaviour of the client-store decorator. This is the layer that actually
/// *enforces* separation (the @-convention only namespaces), so cross-realm access must be denied and
/// listings must never leak other realms' clients — including from the system admin's global view.
/// </summary>
public class RealmScopedClientDbContextTests
{
    #region Fakes

    private sealed class FakeRealmContext : IRealmContext
    {
        private readonly string? _realm;
        public FakeRealmContext(string? realm) => _realm = realm;

        public Task<RealmModel?> GetCurrentRealmAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<RealmModel?>(_realm is null ? null : new RealmModel { Name = _realm });

        public Task<string?> GetCurrentRealmNameAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_realm);
    }

    private sealed class FakeClientDb : IClientDbContext, IClientDbContextModify
    {
        public readonly List<ClientModel> Store = new();

        public Task<ClientModel?> FindClientByIdAsync(string clientId)
            => Task.FromResult<ClientModel?>(Store.FirstOrDefault(c => c.ClientId == clientId));

        public Task AddClientAsync(ClientModel client)
        {
            Store.Add(client);
            return Task.CompletedTask;
        }

        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
        {
            var idx = Store.FindIndex(c => c.ClientId == client.ClientId);
            if (idx < 0) throw new InvalidOperationException("not found");
            Store[idx] = client;
            return Task.CompletedTask;
        }

        public Task RemoveClientAsync(ClientModel client)
        {
            Store.RemoveAll(c => c.ClientId == client.ClientId);
            return Task.CompletedTask;
        }

        public Task<IEnumerable<ClientModel>> GetAllClients()
            => Task.FromResult<IEnumerable<ClientModel>>(Store.ToArray());
    }

    private static RealmScopedClientDbContext Scoped(FakeClientDb backend, string? realm)
        => new RealmScopedClientDbContext(backend, new FakeRealmContext(realm));

    private static FakeClientDb SeededBackend()
    {
        var backend = new FakeClientDb();
        backend.Store.Add(new ClientModel { ClientId = "global-client" });
        backend.Store.Add(new ClientModel { ClientId = "app@xyz" });
        backend.Store.Add(new ClientModel { ClientId = "app@acme" });
        return backend;
    }

    #endregion

    #region Realm admin

    [Fact]
    public async Task AddClient_AppendsCurrentRealm()
    {
        var backend = new FakeClientDb();
        IClientDbContextModify sut = Scoped(backend, "xyz");

        await sut.AddClientAsync(new ClientModel { ClientId = "my-client" });

        Assert.Single(backend.Store);
        Assert.Equal("my-client@xyz", backend.Store[0].ClientId);
    }

    [Fact]
    public async Task GetAllClients_ReturnsOnlyOwnRealm()
    {
        IClientDbContextModify sut = Scoped(SeededBackend(), "xyz");

        var clients = await sut.GetAllClients();

        Assert.Equal(new[] { "app@xyz" }, clients.Select(c => c.ClientId).ToArray());
    }

    [Fact]
    public async Task UpdateClient_OfForeignRealm_IsDenied()
    {
        IClientDbContextModify sut = Scoped(SeededBackend(), "xyz");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.UpdateClientAsync(new ClientModel { ClientId = "app@acme" }));
    }

    [Fact]
    public async Task RemoveClient_OfGlobalNamespace_IsDeniedForRealmAdmin()
    {
        IClientDbContextModify sut = Scoped(SeededBackend(), "xyz");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.RemoveClientAsync(new ClientModel { ClientId = "global-client" }));
    }

    #endregion

    #region System admin (global namespace)

    [Fact]
    public async Task SystemAdmin_GetAllClients_ReturnsOnlyGlobal()
    {
        IClientDbContextModify sut = Scoped(SeededBackend(), realm: null);

        var clients = await sut.GetAllClients();

        Assert.Equal(new[] { "global-client" }, clients.Select(c => c.ClientId).ToArray());
    }

    [Fact]
    public async Task SystemAdmin_AddClient_StaysGlobal()
    {
        var backend = new FakeClientDb();
        IClientDbContextModify sut = Scoped(backend, realm: null);

        await sut.AddClientAsync(new ClientModel { ClientId = "my-client" });

        Assert.Equal("my-client", backend.Store[0].ClientId);
    }

    [Fact]
    public async Task SystemAdmin_UpdateClient_OfRealm_IsDenied()
    {
        IClientDbContextModify sut = Scoped(SeededBackend(), realm: null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.UpdateClientAsync(new ClientModel { ClientId = "app@xyz" }));
    }

    #endregion

    #region Runtime read path (used by IdentityServer)

    [Fact]
    public async Task FindClientById_IsPassThrough_WithFullId()
    {
        IClientDbContext sut = Scoped(SeededBackend(), "xyz");

        // The OIDC pipeline resolves a client of ANOTHER realm by its full id — reads are not scoped.
        var client = await sut.FindClientByIdAsync("app@acme");

        Assert.NotNull(client);
        Assert.Equal("app@acme", client!.ClientId);
    }

    #endregion
}
