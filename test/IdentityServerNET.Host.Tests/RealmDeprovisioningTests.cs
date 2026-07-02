using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Models.UserInteraction;
using IdentityServerNET.Services;
using IdentityServerNET.Services.DbContext;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Pins the realm override mechanism and full realm deprovisioning: deleting a realm removes its
/// realm-scoped clients, roles and resources plus the realm admin, while global entities and other
/// realms are untouched.
/// </summary>
public class RealmDeprovisioningTests
{
    #region RealmScopeOverride

    [Fact]
    public void RealmScopeOverride_BeginSetsAndRestores()
    {
        Assert.False(RealmScopeOverride.IsActive);

        using (RealmScopeOverride.Begin("xyz"))
        {
            Assert.True(RealmScopeOverride.IsActive);
            Assert.Equal("xyz", RealmScopeOverride.Current);

            using (RealmScopeOverride.Begin(null))   // explicit global
            {
                Assert.True(RealmScopeOverride.IsActive);
                Assert.Null(RealmScopeOverride.Current);
            }

            Assert.Equal("xyz", RealmScopeOverride.Current);
        }

        Assert.False(RealmScopeOverride.IsActive);
    }

    [Fact]
    public async Task RealmContext_UsesOverride_WhenActive()
    {
        var ctx = new RealmContext(new HttpContextAccessor(), new FakeRealmDb());

        using (RealmScopeOverride.Begin("xyz"))
        {
            Assert.Equal("xyz", await ctx.GetCurrentRealmNameAsync());
        }

        // No HttpContext and no override -> global (null).
        Assert.Null(await ctx.GetCurrentRealmNameAsync());
    }

    #endregion

    #region DeleteRealmAsync

    [Fact]
    public async Task DeleteRealm_RemovesRealmScopedEntities_AndKeepsGlobalAndOtherRealms()
    {
        var realmDb = new FakeRealmDb();
        realmDb.Store["xyz"] = new RealmModel { Name = "xyz", PrimaryDomain = "foo.com", Domains = { "foo.com" } };

        var clientBackend = new FakeClientDb();
        clientBackend.Store.Add(new ClientModel { ClientId = "app@xyz" });
        clientBackend.Store.Add(new ClientModel { ClientId = "app@acme" });
        clientBackend.Store.Add(new ClientModel { ClientId = "global-client" });

        var roleBackend = new FakeRoleDb();
        roleBackend.Store.Add(Role("role1@xyz"));
        roleBackend.Store.Add(Role("role1@acme"));
        roleBackend.Store.Add(Role("global-role"));

        var resourceBackend = new FakeResourceDb();
        resourceBackend.Apis["api@xyz"] = new ApiResourceModel("api@xyz", "api@xyz");
        resourceBackend.Apis["api@acme"] = new ApiResourceModel("api@acme", "api@acme");
        resourceBackend.Identities["id@xyz"] = new IdentityResourceModel("id@xyz", "id@xyz");

        var userBackend = new FakeUserDb();
        userBackend.Store.Add(new ApplicationUser { UserName = "admin@foo.com", Email = "admin@foo.com" });
        userBackend.Store.Add(new ApplicationUser { UserName = "alice@foo.com", Email = "alice@foo.com" });

        var realmContext = new RealmContext(new HttpContextAccessor(), realmDb);
        var clientDb = new RealmScopedClientDbContext(clientBackend, realmContext);
        var roleDb = new RealmScopedRoleDbContext(roleBackend, realmContext);
        var resourceDb = new RealmScopedResourceDbContext(resourceBackend, realmContext);

        var sut = new RealmProvisioningService(realmDb, roleDb, userBackend, clientDb, resourceDb, new FakePasswordHasher());

        await sut.DeleteRealmAsync(new RealmModel { Name = "xyz" }, CancellationToken.None);

        // Realm-scoped entities of xyz are gone.
        Assert.DoesNotContain(clientBackend.Store, c => c.ClientId == "app@xyz");
        Assert.DoesNotContain(roleBackend.Store, r => r.Name == "role1@xyz");
        Assert.False(resourceBackend.Apis.ContainsKey("api@xyz"));
        Assert.False(resourceBackend.Identities.ContainsKey("id@xyz"));
        Assert.DoesNotContain(userBackend.Store, u => u.UserName == "admin@foo.com");
        Assert.False(realmDb.Store.ContainsKey("xyz"));

        // Other realm + global entities untouched, and the non-admin user is retained.
        Assert.Contains(clientBackend.Store, c => c.ClientId == "app@acme");
        Assert.Contains(clientBackend.Store, c => c.ClientId == "global-client");
        Assert.Contains(roleBackend.Store, r => r.Name == "role1@acme");
        Assert.Contains(roleBackend.Store, r => r.Name == "global-role");
        Assert.True(resourceBackend.Apis.ContainsKey("api@acme"));
        Assert.Contains(userBackend.Store, u => u.UserName == "alice@foo.com");
    }

    #endregion

    #region Fakes

    private static ApplicationRole Role(string idAndName) => new() { Id = idAndName, Name = idAndName };

    private sealed class FakeRealmDb : IRealmDbContext
    {
        public readonly Dictionary<string, RealmModel> Store = new();
        public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken ct)
            => Task.FromResult(Store.TryGetValue(realmName, out var r) ? r : null);
        public Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken ct)
            => Task.FromResult(Store.Values.FirstOrDefault(r => r.Domains.Contains(domain)));
        public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken ct)
            => Task.FromResult<IEnumerable<RealmModel>>(Store.Values.ToArray());
        public Task CreateAsync(RealmModel realm, CancellationToken ct) { Store[realm.Name] = realm; return Task.CompletedTask; }
        public Task UpdateAsync(RealmModel realm, CancellationToken ct) { Store[realm.Name] = realm; return Task.CompletedTask; }
        public Task DeleteAsync(RealmModel realm, CancellationToken ct) { Store.Remove(realm.Name); return Task.CompletedTask; }
    }

    private sealed class FakeClientDb : IClientDbContextModify
    {
        public readonly List<ClientModel> Store = new();
        public Task<ClientModel?> FindClientByIdAsync(string clientId) => Task.FromResult(Store.FirstOrDefault(c => c.ClientId == clientId));
        public Task AddClientAsync(ClientModel client) { Store.Add(client); return Task.CompletedTask; }
        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? p = null) => Task.CompletedTask;
        public Task RemoveClientAsync(ClientModel client) { Store.RemoveAll(c => c.ClientId == client.ClientId); return Task.CompletedTask; }
        public Task<IEnumerable<ClientModel>> GetAllClients() => Task.FromResult<IEnumerable<ClientModel>>(Store.ToArray());
    }

    private sealed class FakeRoleDb : IRoleDbContext, IAdminRoleDbContext
    {
        public readonly List<ApplicationRole> Store = new();
        public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken ct) { Store.Add(role); return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken ct) { Store.RemoveAll(r => r.Id == role.Id); return Task.FromResult(IdentityResult.Success); }
        public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken ct) => Task.FromResult(Store.FirstOrDefault(r => r.Id == roleId));
        public Task<ApplicationRole?> FindByNameAsync(string name, CancellationToken ct) => Task.FromResult(Store.FirstOrDefault(r => string.Equals(r.Name, name, StringComparison.OrdinalIgnoreCase)));
        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string prop, T val, CancellationToken ct) => Task.FromResult(val);
        public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken ct) => Task.FromResult<IEnumerable<ApplicationRole>>(Store.Skip(skip).Take(limit).ToArray());
        public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken ct) => Task.FromResult<IEnumerable<ApplicationRole>>(Store.ToArray());
    }

    private sealed class FakeResourceDb : IResourceDbContextModify
    {
        public readonly Dictionary<string, ApiResourceModel> Apis = new();
        public readonly Dictionary<string, IdentityResourceModel> Identities = new();
        public Task<ApiResourceModel?> FindApiResourceAsync(string name) => Task.FromResult(Apis.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> s) => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IEnumerable<ApiResourceModel>> GetAllApiResources() => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IdentityResourceModel?> FindIdentityResource(string name) => Task.FromResult(Identities.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources() => Task.FromResult<IEnumerable<IdentityResourceModel>>(Identities.Values.ToArray());
        public Task AddApiResourceAsync(ApiResourceModel r) { Apis[r.Name] = r; return Task.CompletedTask; }
        public Task UpdateApiResourceAsync(ApiResourceModel r, IEnumerable<string>? p = null) { Apis[r.Name] = r; return Task.CompletedTask; }
        public Task RemoveApiResourceAsync(ApiResourceModel r) { Apis.Remove(r.Name); return Task.CompletedTask; }
        public Task AddIdentityResourceAsync(IdentityResourceModel r) { Identities[r.Name] = r; return Task.CompletedTask; }
        public Task UpdateIdentityResourceAsync(IdentityResourceModel r, IEnumerable<string>? p = null) { Identities[r.Name] = r; return Task.CompletedTask; }
        public Task RemoveIdentityResourceAsync(IdentityResourceModel r) { Identities.Remove(r.Name); return Task.CompletedTask; }
    }

    private sealed class FakeUserDb : IUserDbContext
    {
        public readonly List<ApplicationUser> Store = new();
        public string DefaultAdminLogin => "";
        public UserDbContextConfiguration ContextConfiguration => new();
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) { Store.Add(user); return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) { Store.RemoveAll(u => u.UserName == user.UserName); return Task.FromResult(IdentityResult.Success); }
        public Task<ApplicationUser?> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(Store.FirstOrDefault(u => u.Id == id));
        public Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken ct) => Task.FromResult(Store.FirstOrDefault(u => string.Equals(u.UserName, name, StringComparison.OrdinalIgnoreCase)));
        public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Store.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string prop, T val, CancellationToken ct) => Task.FromResult(val);
        public Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo info, object val, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher<ApplicationUser>
    {
        public string HashPassword(ApplicationUser user, string password) => "hashed";
        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword) => PasswordVerificationResult.Success;
    }

    #endregion
}
