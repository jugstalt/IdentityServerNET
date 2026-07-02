using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Services;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Pins the domain-based user isolation. Users carry no @realm suffix, so a realm admin sees/creates
/// only users in its realm's domains; the system admin sees global users plus realm admin accounts.
/// </summary>
public class RealmUserScopeTests
{
    #region Fakes

    private sealed class FakeRealmContext : IRealmContext
    {
        private readonly RealmModel? _realm;
        public FakeRealmContext(RealmModel? realm) => _realm = realm;

        public Task<RealmModel?> GetCurrentRealmAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_realm);

        public Task<string?> GetCurrentRealmNameAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_realm?.Name);
    }

    // Minimal realm store: only the members RealmUserScope uses (GetAll, FindByDomain).
    private sealed class FakeRealmDb : IRealmDbContext
    {
        private readonly List<RealmModel> _realms;
        public FakeRealmDb(params RealmModel[] realms) => _realms = realms.ToList();

        public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<RealmModel>>(_realms.ToArray());

        public Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken)
            => Task.FromResult<RealmModel?>(_realms.FirstOrDefault(
                r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase))));

        public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken)
            => Task.FromResult<RealmModel?>(_realms.FirstOrDefault(r => r.Name == realmName));

        public Task CreateAsync(RealmModel realm, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static readonly RealmModel Xyz = new()
    {
        Name = "xyz",
        PrimaryDomain = "foo.com",
        Domains = new List<string> { "foo.com", "bar.org" }
    };

    private static readonly RealmModel Acme = new()
    {
        Name = "acme",
        PrimaryDomain = "acme.com",
        Domains = new List<string> { "acme.com" }
    };

    private static RealmUserScope Scope(RealmModel? current)
        => new RealmUserScope(new FakeRealmContext(current), new FakeRealmDb(Xyz, Acme));

    private static ApplicationUser User(string name) => new() { UserName = name, Email = name };

    private static readonly ApplicationUser[] AllUsers =
    {
        User("alice@foo.com"),   // xyz
        User("bob@bar.org"),     // xyz
        User("carol@acme.com"),  // acme
        User("root@is.net"),     // global (is.net owned by no realm)
    };

    #endregion

    #region Listing

    [Fact]
    public async Task RealmAdmin_SeesOnlyUsersInItsDomains()
    {
        var result = await Scope(Xyz).FilterToCurrentRealmAsync(AllUsers, CancellationToken.None);

        Assert.Equal(
            new[] { "alice@foo.com", "bob@bar.org" },
            result.Select(u => u.UserName).ToArray());
    }

    [Fact]
    public async Task SystemAdmin_SeesGlobalUsersAndRealmAdmins()
    {
        // Xyz primary domain = foo.com → realm admin is admin@foo.com
        // Acme primary domain = acme.com → realm admin is admin@acme.com
        var all = new[]
        {
            User("alice@foo.com"),   // xyz realm user  → hidden
            User("bob@bar.org"),     // xyz realm user  → hidden
            User("admin@foo.com"),   // xyz realm admin → visible
            User("carol@acme.com"),  // acme realm user → hidden
            User("admin@acme.com"),  // acme realm admin → visible
            User("root@is.net"),     // global user      → visible
        };

        var result = await Scope(null).FilterToCurrentRealmAsync(all, CancellationToken.None);
        var names = result.Select(u => u.UserName).OrderBy(x => x).ToArray();

        Assert.Equal(
            new[] { "admin@acme.com", "admin@foo.com", "root@is.net" },
            names);
    }

    #endregion

    #region Create validation

    [Fact]
    public async Task RealmAdmin_MayCreateUserInOwnDomain()
    {
        Assert.Null(await Scope(Xyz).ValidateUserInCurrentRealmAsync("new@bar.org", CancellationToken.None));
    }

    [Fact]
    public async Task RealmAdmin_MayNotCreateUserInForeignDomain()
    {
        Assert.NotNull(await Scope(Xyz).ValidateUserInCurrentRealmAsync("new@acme.com", CancellationToken.None));
    }

    [Fact]
    public async Task RealmAdmin_MayNotCreateUserInUnassignedDomain()
    {
        Assert.NotNull(await Scope(Xyz).ValidateUserInCurrentRealmAsync("new@is.net", CancellationToken.None));
    }

    [Fact]
    public async Task SystemAdmin_MayCreateUserInGlobalDomain()
    {
        Assert.Null(await Scope(null).ValidateUserInCurrentRealmAsync("new@is.net", CancellationToken.None));
    }

    [Fact]
    public async Task SystemAdmin_MayNotCreateUserInRealmDomain()
    {
        Assert.NotNull(await Scope(null).ValidateUserInCurrentRealmAsync("new@foo.com", CancellationToken.None));
    }

    #endregion
}
