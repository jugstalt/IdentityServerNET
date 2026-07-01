using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Pins the realm isolation behaviour of the role-store decorator: creating a role appends the
/// caller's realm, listings never leak other realms, and delete/update of a foreign realm's role
/// fails closed.
/// </summary>
public class RealmScopedRoleDbContextTests
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

    private sealed class FakeRoleDb : IRoleDbContext, IAdminRoleDbContext
    {
        public readonly List<ApplicationRole> Store = new();

        public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            Store.Add(role);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            Store.RemoveAll(r => r.Id == role.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationRole?>(Store.FirstOrDefault(r => r.Id == roleId));

        public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
            => Task.FromResult<ApplicationRole?>(Store.FirstOrDefault(r => string.Equals(r.Name, normalizedRoleName, StringComparison.OrdinalIgnoreCase)));

        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
            => Task.FromResult(IdentityResult.Success);

        public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellationToken)
            => Task.FromResult(propertyValue);

        public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<ApplicationRole>>(Store.Skip(skip).Take(limit).ToArray());

        public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<ApplicationRole>>(Store.Where(r => r.Name?.Contains(term) == true).ToArray());
    }

    private static RealmScopedRoleDbContext Scoped(FakeRoleDb backend, string? realm)
        => new RealmScopedRoleDbContext(backend, new FakeRealmContext(realm));

    private static ApplicationRole Role(string idAndName)
        => new ApplicationRole { Id = idAndName, Name = idAndName };

    private static FakeRoleDb SeededBackend()
    {
        var backend = new FakeRoleDb();
        backend.Store.Add(Role("admin-role"));
        backend.Store.Add(Role("role1@xyz"));
        backend.Store.Add(Role("role2@acme"));
        return backend;
    }

    #endregion

    #region Realm admin

    [Fact]
    public async Task Create_AppendsRealm_ToNameAndId()
    {
        var backend = new FakeRoleDb();
        IRoleDbContext sut = Scoped(backend, "xyz");

        await sut.CreateAsync(Role("role1"), CancellationToken.None);

        Assert.Single(backend.Store);
        Assert.Equal("role1@xyz", backend.Store[0].Name);
        Assert.Equal("role1@xyz", backend.Store[0].Id);
    }

    [Fact]
    public async Task GetRoles_ReturnsOnlyOwnRealm()
    {
        IAdminRoleDbContext sut = Scoped(SeededBackend(), "xyz");

        var roles = await sut.GetRolesAsync(100, 0, CancellationToken.None);

        Assert.Equal(new[] { "role1@xyz" }, roles.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task Delete_OfForeignRealm_FailsClosed()
    {
        var backend = SeededBackend();
        IRoleDbContext sut = Scoped(backend, "xyz");

        var result = await sut.DeleteAsync(Role("role2@acme"), CancellationToken.None);

        Assert.False(result.Succeeded);
        Assert.Contains(backend.Store, r => r.Id == "role2@acme"); // untouched
    }

    [Fact]
    public async Task Update_OfGlobalRole_IsDeniedForRealmAdmin()
    {
        IRoleDbContext sut = Scoped(SeededBackend(), "xyz");

        var result = await sut.UpdateAsync(Role("admin-role"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    #endregion

    #region System admin (global namespace)

    [Fact]
    public async Task SystemAdmin_GetRoles_ReturnsOnlyGlobal()
    {
        IAdminRoleDbContext sut = Scoped(SeededBackend(), realm: null);

        var roles = await sut.GetRolesAsync(100, 0, CancellationToken.None);

        Assert.Equal(new[] { "admin-role" }, roles.Select(r => r.Name).ToArray());
    }

    [Fact]
    public async Task SystemAdmin_Create_StaysGlobal()
    {
        var backend = new FakeRoleDb();
        IRoleDbContext sut = Scoped(backend, realm: null);

        await sut.CreateAsync(Role("admin-role"), CancellationToken.None);

        Assert.Equal("admin-role", backend.Store[0].Name);
    }

    [Fact]
    public async Task SystemAdmin_Delete_OfRealmRole_IsDenied()
    {
        IRoleDbContext sut = Scoped(SeededBackend(), realm: null);

        var result = await sut.DeleteAsync(Role("role1@xyz"), CancellationToken.None);

        Assert.False(result.Succeeded);
    }

    #endregion
}
