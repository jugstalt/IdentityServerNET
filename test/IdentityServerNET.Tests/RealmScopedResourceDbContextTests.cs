using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.DbContext;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Pins the realm isolation behaviour of the resource-store decorator: creating a resource appends
/// the caller's realm to its name, and update/remove of a foreign realm's resource fails closed.
/// Reads stay pass-through (IdentityServer resolves resources by their full name).
/// </summary>
public class RealmScopedResourceDbContextTests
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

    private sealed class FakeResourceDb : IResourceDbContextModify
    {
        public readonly Dictionary<string, ApiResourceModel> Apis = new();
        public readonly Dictionary<string, IdentityResourceModel> Identities = new();

        public Task<ApiResourceModel?> FindApiResourceAsync(string name)
            => Task.FromResult(Apis.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
            => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
            => Task.FromResult<IEnumerable<ApiResourceModel>>(Apis.Values.ToArray());
        public Task<IdentityResourceModel?> FindIdentityResource(string name)
            => Task.FromResult(Identities.TryGetValue(name, out var r) ? r : null);
        public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
            => Task.FromResult<IEnumerable<IdentityResourceModel>>(Identities.Values.ToArray());

        public Task AddApiResourceAsync(ApiResourceModel apiResource) { Apis[apiResource.Name] = apiResource; return Task.CompletedTask; }
        public Task UpdateApiResourceAsync(ApiResourceModel resource, IEnumerable<string>? propertyNames = null) { Apis[resource.Name] = resource; return Task.CompletedTask; }
        public Task RemoveApiResourceAsync(ApiResourceModel apiResource) { Apis.Remove(apiResource.Name); return Task.CompletedTask; }
        public Task AddIdentityResourceAsync(IdentityResourceModel identityResource) { Identities[identityResource.Name] = identityResource; return Task.CompletedTask; }
        public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string>? propertyNames = null) { Identities[identityResource.Name] = identityResource; return Task.CompletedTask; }
        public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource) { Identities.Remove(identityResource.Name); return Task.CompletedTask; }
    }

    private static RealmScopedResourceDbContext Scoped(FakeResourceDb backend, string? realm)
        => new RealmScopedResourceDbContext(backend, new FakeRealmContext(realm));

    private static ApiResourceModel Api(string name) => new ApiResourceModel(name, name);
    private static IdentityResourceModel Identity(string name) => new IdentityResourceModel(name, name);

    #endregion

    [Fact]
    public async Task AddApiResource_AppendsCurrentRealm()
    {
        var backend = new FakeResourceDb();
        IResourceDbContextModify sut = Scoped(backend, "xyz");

        await sut.AddApiResourceAsync(Api("myapi"));

        Assert.True(backend.Apis.ContainsKey("myapi@xyz"));
    }

    [Fact]
    public async Task AddIdentityResource_AppendsCurrentRealm()
    {
        var backend = new FakeResourceDb();
        IResourceDbContextModify sut = Scoped(backend, "xyz");

        await sut.AddIdentityResourceAsync(Identity("profile-ext"));

        Assert.True(backend.Identities.ContainsKey("profile-ext@xyz"));
    }

    [Fact]
    public async Task SystemAdmin_AddApiResource_StaysGlobal()
    {
        var backend = new FakeResourceDb();
        IResourceDbContextModify sut = Scoped(backend, realm: null);

        await sut.AddApiResourceAsync(Api("myapi"));

        Assert.True(backend.Apis.ContainsKey("myapi"));
    }

    [Fact]
    public async Task UpdateApiResource_OfForeignRealm_IsDenied()
    {
        IResourceDbContextModify sut = Scoped(new FakeResourceDb(), "xyz");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.UpdateApiResourceAsync(Api("otherapi@acme")));
    }

    [Fact]
    public async Task RemoveApiResource_OfGlobal_IsDeniedForRealmAdmin()
    {
        IResourceDbContextModify sut = Scoped(new FakeResourceDb(), "xyz");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => sut.RemoveApiResourceAsync(Api("secrets-vault")));
    }

    [Fact]
    public async Task FindApiResource_IsPassThrough_WithFullName()
    {
        var backend = new FakeResourceDb();
        backend.Apis["otherapi@acme"] = Api("otherapi@acme");
        IResourceDbContext sut = Scoped(backend, "xyz");

        var resource = await sut.FindApiResourceAsync("otherapi@acme");

        Assert.NotNull(resource);
    }
}
