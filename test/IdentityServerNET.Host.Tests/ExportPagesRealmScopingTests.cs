using IdentityServer.Areas.Admin.Pages.Clients;
using IdentityServer.Areas.Admin.Pages.Resources;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.DbContext;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Security tests for the "Export DB" admin pages (Clients/ExportClientDb, Resources/ExportResourceDb).
/// A realm admin must never be able to export another realm's (or the global namespace's) clients or
/// resources by hitting these pages.
///
/// The two pages behave differently under the hood: <c>IClientDbContext</c> is always resolved to
/// <see cref="RealmScopedClientDbContext"/>, whose own <c>GetAllClients()</c> already realm-filters -
/// so the page's own filter is a defense-in-depth backstop for clients. <c>IResourceDbContext</c>'s
/// <see cref="RealmScopedResourceDbContext"/>, by contrast, is a deliberate pass-through on reads
/// (IdentityServer needs to resolve resources across all realms at runtime) - for resources, the
/// page's own filter is the *only* thing preventing a cross-realm export. These tests wire up the
/// real decorators (not just page-level fakes) so a regression in either layer would be caught.
/// </summary>
public class ExportPagesRealmScopingTests
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
        public Task<ClientModel?> FindClientByIdAsync(string clientId) => Task.FromResult(Store.FirstOrDefault(c => c.ClientId == clientId));
        public Task AddClientAsync(ClientModel client) { Store.Add(client); return Task.CompletedTask; }
        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null) => Task.CompletedTask;
        public Task RemoveClientAsync(ClientModel client) => Task.CompletedTask;
        public Task<IEnumerable<ClientModel>> GetAllClients() => Task.FromResult<IEnumerable<ClientModel>>(Store.ToArray());
    }

    private sealed class FakeExportClientDb : IExportClientDbContext
    {
        public readonly List<ClientModel> Exported = new();
        public bool FlushCalled;
        public Task FlushDb() { FlushCalled = true; Exported.Clear(); return Task.CompletedTask; }
        public Task<ClientModel?> FindClientByIdAsync(string clientId) => Task.FromResult(Exported.FirstOrDefault(c => c.ClientId == clientId));
        public Task AddClientAsync(ClientModel client) { Exported.Add(client); return Task.CompletedTask; }
        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null) => Task.CompletedTask;
        public Task RemoveClientAsync(ClientModel client) => Task.CompletedTask;
        public Task<IEnumerable<ClientModel>> GetAllClients() => Task.FromResult<IEnumerable<ClientModel>>(Exported.ToArray());
    }

    private sealed class FakeResourceDb : IResourceDbContext, IResourceDbContextModify
    {
        public readonly Dictionary<string, ApiResourceModel> Apis = new();
        public readonly Dictionary<string, IdentityResourceModel> Identities = new();
        public Task<ApiResourceModel?> FindApiResourceAsync(string name) => Task.FromResult(Apis.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames) => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IEnumerable<ApiResourceModel>> GetAllApiResources() => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IdentityResourceModel?> FindIdentityResource(string name) => Task.FromResult(Identities.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources() => Task.FromResult<IEnumerable<IdentityResourceModel>>(Identities.Values.ToArray());
        public Task AddApiResourceAsync(ApiResourceModel r) { Apis[r.Name] = r; return Task.CompletedTask; }
        public Task UpdateApiResourceAsync(ApiResourceModel r, IEnumerable<string>? p = null) => Task.CompletedTask;
        public Task RemoveApiResourceAsync(ApiResourceModel r) => Task.CompletedTask;
        public Task AddIdentityResourceAsync(IdentityResourceModel r) { Identities[r.Name] = r; return Task.CompletedTask; }
        public Task UpdateIdentityResourceAsync(IdentityResourceModel r, IEnumerable<string>? p = null) => Task.CompletedTask;
        public Task RemoveIdentityResourceAsync(IdentityResourceModel r) => Task.CompletedTask;
    }

    private sealed class FakeExportResourceDb : IExportResourceDbContext
    {
        public readonly List<ApiResourceModel> ExportedApis = new();
        public readonly List<IdentityResourceModel> ExportedIdentities = new();
        public bool FlushCalled;
        public Task FlushDb() { FlushCalled = true; ExportedApis.Clear(); ExportedIdentities.Clear(); return Task.CompletedTask; }
        public Task<ApiResourceModel?> FindApiResourceAsync(string name) => Task.FromResult(ExportedApis.FirstOrDefault(a => a.Name == name));
        public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames) => Task.FromResult<IEnumerable<ApiResourceModel>>(ExportedApis.ToArray());
        public Task<IEnumerable<ApiResourceModel>> GetAllApiResources() => Task.FromResult<IEnumerable<ApiResourceModel>>(ExportedApis.ToArray());
        public Task<IdentityResourceModel?> FindIdentityResource(string name) => Task.FromResult(ExportedIdentities.FirstOrDefault(i => i.Name == name));
        public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources() => Task.FromResult<IEnumerable<IdentityResourceModel>>(ExportedIdentities.ToArray());
        public Task AddApiResourceAsync(ApiResourceModel r) { ExportedApis.Add(r); return Task.CompletedTask; }
        public Task UpdateApiResourceAsync(ApiResourceModel r, IEnumerable<string>? p = null) => Task.CompletedTask;
        public Task RemoveApiResourceAsync(ApiResourceModel r) => Task.CompletedTask;
        public Task AddIdentityResourceAsync(IdentityResourceModel r) { ExportedIdentities.Add(r); return Task.CompletedTask; }
        public Task UpdateIdentityResourceAsync(IdentityResourceModel r, IEnumerable<string>? p = null) => Task.CompletedTask;
        public Task RemoveIdentityResourceAsync(IdentityResourceModel r) => Task.CompletedTask;
    }

    #endregion

    #region ExportClientDb

    [Fact]
    public async Task ExportClientDb_RealmAdmin_OnlyExportsOwnRealmClients()
    {
        var backend = new FakeClientDb();
        backend.Store.Add(new ClientModel { ClientId = "global-client" });
        backend.Store.Add(new ClientModel { ClientId = "app@xyz" });
        backend.Store.Add(new ClientModel { ClientId = "app@acme" });

        var scopedSource = new RealmScopedClientDbContext(backend, new FakeRealmContext("xyz"));
        var exportTarget = new FakeExportClientDb();
        var page = new ExportClientDbModel(scopedSource, exportTarget, new FakeRealmContext("xyz"));

        await page.OnGetAsync();

        Assert.Equal(new[] { "app@xyz" }, exportTarget.Exported.Select(c => c.ClientId));
    }

    [Fact]
    public async Task ExportClientDb_SystemAdmin_NeverExportsRealmNamespacedClients()
    {
        // Characterizes the current (pre-existing, unrelated) behavior: RealmScopedClientDbContext's
        // own GetAllClients() only returns non-namespaced clients for the global namespace, so the
        // system admin can never export realm-scoped clients through this page - regardless of this
        // page's own filter. Not a security issue (fails closed), but worth pinning explicitly.
        var backend = new FakeClientDb();
        backend.Store.Add(new ClientModel { ClientId = "global-client" });
        backend.Store.Add(new ClientModel { ClientId = "app@xyz" });

        var scopedSource = new RealmScopedClientDbContext(backend, new FakeRealmContext(null));
        var exportTarget = new FakeExportClientDb();
        var page = new ExportClientDbModel(scopedSource, exportTarget, new FakeRealmContext(null));

        await page.OnGetAsync();

        Assert.Equal(new[] { "global-client" }, exportTarget.Exported.Select(c => c.ClientId));
    }

    #endregion

    #region ExportResourceDb

    [Fact]
    public async Task ExportResourceDb_RealmAdmin_OnlyExportsOwnRealmResources()
    {
        // Unlike clients, RealmScopedResourceDbContext.GetAllApiResources()/GetAllIdentityResources()
        // are pure pass-throughs with no realm filtering at all (by design, so the OIDC runtime can
        // resolve resources across every realm) - so this page's own filter is the only thing
        // standing between a realm admin and every other realm's resources.
        var backend = new FakeResourceDb();
        backend.Apis["global-api"] = new ApiResourceModel { Name = "global-api" };
        backend.Apis["api@xyz"] = new ApiResourceModel { Name = "api@xyz" };
        backend.Apis["api@acme"] = new ApiResourceModel { Name = "api@acme" };
        backend.Identities["global-identity"] = new IdentityResourceModel { Name = "global-identity" };
        backend.Identities["identity@xyz"] = new IdentityResourceModel { Name = "identity@xyz" };
        backend.Identities["identity@acme"] = new IdentityResourceModel { Name = "identity@acme" };

        var scopedSource = new RealmScopedResourceDbContext(backend, new FakeRealmContext("xyz"));
        var exportTarget = new FakeExportResourceDb();
        var page = new ExportResourceDbModel(scopedSource, exportTarget, new FakeRealmContext("xyz"));

        await page.OnGetAsync();

        Assert.Equal(new[] { "api@xyz" }, exportTarget.ExportedApis.Select(a => a.Name));
        Assert.Equal(new[] { "identity@xyz" }, exportTarget.ExportedIdentities.Select(i => i.Name));
    }

    [Fact]
    public async Task ExportResourceDb_SystemAdmin_ExportsEverything()
    {
        var backend = new FakeResourceDb();
        backend.Apis["global-api"] = new ApiResourceModel { Name = "global-api" };
        backend.Apis["api@xyz"] = new ApiResourceModel { Name = "api@xyz" };

        var scopedSource = new RealmScopedResourceDbContext(backend, new FakeRealmContext(null));
        var exportTarget = new FakeExportResourceDb();
        var page = new ExportResourceDbModel(scopedSource, exportTarget, new FakeRealmContext(null));

        await page.OnGetAsync();

        Assert.Equal(
            new[] { "api@xyz", "global-api" },
            exportTarget.ExportedApis.Select(a => a.Name).OrderBy(n => n));
    }

    #endregion
}
