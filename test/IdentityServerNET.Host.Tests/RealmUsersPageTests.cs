using IdentityServer.Areas.Admin.Pages.Realms.EditRealm;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using IdentityServerNET.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Tests for the realm "Users" admin page (<c>/Admin/Realms/EditRealm/RealmUsers</c>). The system
/// admin must see every user belonging to the realm (not just the ones it can open), with the
/// realm's admin account listed first and every other user rendered non-editable - so what's
/// clickable in the list always matches what <c>EditUser</c> will actually let the caller open
/// (via the same <see cref="IRealmUserScope"/> rule), instead of 404ing with "Unable to load user"
/// for a link that looked clickable.
/// </summary>
public class RealmUsersPageTests
{
    #region Fakes

    private sealed class FakeRealmDb : IRealmDbContext
    {
        public readonly List<RealmModel> Realms = new();
        public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken ct) => Task.FromResult(Realms.FirstOrDefault(r => r.Name == realmName));
        public Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken ct) => Task.FromResult(Realms.FirstOrDefault(r => r.Domains.Contains(domain, StringComparer.OrdinalIgnoreCase)));
        public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken ct) => Task.FromResult<IEnumerable<RealmModel>>(Realms.ToArray());
        public Task CreateAsync(RealmModel realm, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(RealmModel realm, CancellationToken ct) => Task.CompletedTask;
        public Task DeleteAsync(RealmModel realm, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeAdminUserDb : IAdminUserDbContext
    {
        public readonly List<ApplicationUser> Users = new();
        public string DefaultAdminLogin => "";
        public UserDbContextConfiguration ContextConfiguration => new();
        public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken ct) => Task.FromResult<IEnumerable<ApplicationUser>>(Users.Skip(skip).Take(limit).ToArray());
        public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken ct) => Task.FromResult<IEnumerable<ApplicationUser>>(Users.ToArray());
        public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken ct) { Users.Add(user); return Task.FromResult(IdentityResult.Success); }
        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken ct) { Users.RemoveAll(u => u.Id == user.Id); return Task.FromResult(IdentityResult.Success); }
        public Task<ApplicationUser?> FindByIdAsync(string id, CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
        public Task<ApplicationUser?> FindByNameAsync(string name, CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.UserName, name, StringComparison.OrdinalIgnoreCase)));
        public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken ct) => Task.FromResult(Users.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken ct) => Task.FromResult(IdentityResult.Success);
        public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string prop, T val, CancellationToken ct) => Task.FromResult(val);
        public Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo info, object val, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class FakeRealmContext : IRealmContext
    {
        private readonly RealmModel? _realm;
        public FakeRealmContext(RealmModel? realm) => _realm = realm;
        public Task<RealmModel?> GetCurrentRealmAsync(CancellationToken ct = default) => Task.FromResult(_realm);
        public Task<string?> GetCurrentRealmNameAsync(CancellationToken ct = default) => Task.FromResult(_realm?.Name);
    }

    private static (FakeRealmDb realmDb, FakeAdminUserDb userDb) SeededBackends()
    {
        var realmDb = new FakeRealmDb();
        realmDb.Realms.Add(new RealmModel { Name = "esn", PrimaryDomain = "esn.at", Domains = new List<string> { "esn.at" } });

        var userDb = new FakeAdminUserDb();
        userDb.Users.Add(new ApplicationUser { Id = "1", UserName = "test@esn.at" });   // regular realm user
        userDb.Users.Add(new ApplicationUser { Id = "2", UserName = "admin@esn.at" });  // realm admin account
        userDb.Users.Add(new ApplicationUser { Id = "3", UserName = "someone@other.example" }); // unrelated realm, excluded by domain

        return (realmDb, userDb);
    }

    #endregion

    [Fact]
    public async Task SystemAdmin_SeesEveryRealmUser_AdminFirstAndEditable_OthersDisabled()
    {
        var (realmDb, userDb) = SeededBackends();
        // realm == null on IRealmContext is exactly how RealmUserScope recognizes the system admin.
        var realmUserScope = new RealmUserScope(new FakeRealmContext(null), realmDb);

        var page = new RealmUsersModel(realmDb, userDb, realmUserScope);
        await page.OnGetAsync("esn");

        Assert.Equal(2, page.Users.Count); // only esn.at users - the unrelated-realm user is excluded
        Assert.Equal("admin@esn.at", page.Users[0].User.UserName);
        Assert.True(page.Users[0].IsEditable, "The realm admin account must stay editable (recovery access).");
        Assert.Equal("test@esn.at", page.Users[1].User.UserName);
        Assert.False(page.Users[1].IsEditable, "A regular realm user must not be editable by the system admin.");
    }

    [Fact]
    public async Task RealmAdmin_SeesOwnRealmUsers_AllEditable()
    {
        var (realmDb, userDb) = SeededBackends();
        var realmUserScope = new RealmUserScope(new FakeRealmContext(realmDb.Realms.Single()), realmDb);

        var page = new RealmUsersModel(realmDb, userDb, realmUserScope);
        await page.OnGetAsync("esn");

        Assert.Equal(2, page.Users.Count);
        Assert.All(page.Users, item => Assert.True(item.IsEditable));
    }

    [Fact]
    public async Task NoRealmUserScopeRegistered_TreatsEveryUserAsEditable()
    {
        // Mirrors EditUserPageModel.LoadCurrentApplicationUserAsync's own fallback: if the realm
        // feature's scoping service isn't registered at all, no restriction is applied anywhere.
        var (realmDb, userDb) = SeededBackends();

        var page = new RealmUsersModel(realmDb, userDb, realmUserScope: null);
        await page.OnGetAsync("esn");

        Assert.Equal(2, page.Users.Count);
        Assert.All(page.Users, item => Assert.True(item.IsEditable));
    }
}
