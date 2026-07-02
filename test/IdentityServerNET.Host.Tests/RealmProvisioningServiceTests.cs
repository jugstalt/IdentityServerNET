using IdentityServerNET;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using IdentityServerNET.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Pins realm provisioning: creating a realm stores it, provisions the realm-scoped admin roles
/// (role@realm) and a realm admin user admin@{primary-domain} carrying exactly those roles.
/// </summary>
public class RealmProvisioningServiceTests
{
    #region Fakes

    private sealed class FakeRealmDb : IRealmDbContext
    {
        public readonly List<RealmModel> Created = new();
        public Task CreateAsync(RealmModel realm, CancellationToken cancellationToken) { Created.Add(realm); return Task.CompletedTask; }
        public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken) => Task.FromResult<RealmModel?>(null);
        public Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken) => Task.FromResult<RealmModel?>(null);
        public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken) => Task.FromResult<IEnumerable<RealmModel>>(Created.ToArray());
        public Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeRoleDb : IRoleDbContext
    {
        public readonly List<ApplicationRole> Created = new();
        public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken) { Created.Add(role); return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken) => Task.FromResult<ApplicationRole?>(null);
        public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken) => Task.FromResult<ApplicationRole?>(null);
        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellation) => Task.FromResult(propertyValue);
    }

    private sealed class FakeUserDb : IUserDbContext
    {
        public ApplicationUser? Created { get; private set; }
        public string DefaultAdminLogin => "";
        public UserDbContextConfiguration ContextConfiguration => new();
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) { Created = user; return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) => Task.FromResult<ApplicationUser?>(null);
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => Task.FromResult(IdentityResult.Success);
        public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation) => Task.FromResult(propertyValue);
        public Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation) => Task.CompletedTask;
    }

    private sealed class FakePasswordHasher : IPasswordHasher<ApplicationUser>
    {
        public string HashPassword(ApplicationUser user, string password) => "hashed:" + password;
        public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
            => PasswordVerificationResult.Success;
    }

    #endregion

    [Fact]
    public async Task CreateRealm_StoresRealm_ProvisionsRolesAndAdmin()
    {
        var realmDb = new FakeRealmDb();
        var roleDb = new FakeRoleDb();
        var userDb = new FakeUserDb();
        var sut = new RealmProvisioningService(realmDb, roleDb, userDb, new FakePasswordHasher());

        var realm = new RealmModel
        {
            Name = "xyz",
            PrimaryDomain = "foo.com",
            Domains = new List<string> { "foo.com", "bar.org" }
        };

        var result = await sut.CreateRealmAsync(realm, CancellationToken.None);

        // Realm stored.
        Assert.Single(realmDb.Created);

        // Exactly the realm-delegated roles, realm-scoped.
        var expectedRoles = KnownRoles.RealmDelegatedRoles.Select(r => r + "@xyz").OrderBy(x => x).ToArray();
        Assert.Equal(expectedRoles, roleDb.Created.Select(r => r.Name).OrderBy(x => x).ToArray());

        // Realm admin user, anchored to the primary domain, carrying those roles.
        Assert.Equal("admin@foo.com", result.AdminUserName);
        Assert.NotNull(userDb.Created);
        Assert.Equal("admin@foo.com", userDb.Created!.UserName);
        Assert.True(userDb.Created.EmailConfirmed);
        Assert.Equal(expectedRoles, userDb.Created.Roles!.OrderBy(x => x).ToArray());

        // A password was generated and hashed.
        Assert.False(string.IsNullOrEmpty(result.AdminPassword));
        Assert.False(string.IsNullOrEmpty(userDb.Created.PasswordHash));
    }

    [Fact]
    public async Task CreateRealm_InvalidRealm_Throws()
    {
        var sut = new RealmProvisioningService(new FakeRealmDb(), new FakeRoleDb(), new FakeUserDb(), new FakePasswordHasher());

        // Primary domain not part of the domain set is normalized in; but an empty name is invalid.
        var realm = new RealmModel { Name = "", PrimaryDomain = "foo.com", Domains = new List<string> { "foo.com" } };

        await Assert.ThrowsAsync<ArgumentException>(() => sut.CreateRealmAsync(realm, CancellationToken.None));
    }
}
