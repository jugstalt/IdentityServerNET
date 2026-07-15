using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Realms.EditRealm;

public class RealmUsersModel : EditRealmPageModel
{
    private readonly IUserDbContext _userDb;
    private readonly IRealmUserScope _realmUserScope;

    public RealmUsersModel(IRealmDbContext realmDb, IUserDbContext userDb, IRealmUserScope realmUserScope = null) : base(realmDb)
    {
        _userDb = userDb;
        _realmUserScope = realmUserScope;
    }

    // All users in the realm's domains, alongside whether the current caller may actually open
    // them. A realm admin can open every user shown here (its own domains, same rule as the
    // listing filter below). The system admin sees every user too, but IRealmUserScope only lets
    // it open the realm's admin account (recovery access) or global-domain users - shown first,
    // the rest rendered read-only so what's clickable here matches what EditUser will actually load.
    public IReadOnlyList<RealmUserListItem> Users { get; private set; } = Array.Empty<RealmUserListItem>();

    public async Task<IActionResult> OnGetAsync(string name)
    {
        await LoadCurrentRealmAsync(name);
        if (CurrentRealm == null) return NotFound();

        if (_userDb is IAdminUserDbContext adminUserDb)
        {
            var domains = new HashSet<string>(
                CurrentRealm.Domains ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var allUsers = (await adminUserDb.GetUsersAsync(1000, 0, CancellationToken.None))
                .Where(u => u != null && domains.Contains(DomainOf(u)))
                .ToArray();

            var editableIds = _realmUserScope != null
                ? new HashSet<string>((await _realmUserScope.FilterToCurrentRealmAsync(allUsers, CancellationToken.None)).Select(u => u.Id))
                : new HashSet<string>(allUsers.Select(u => u.Id));

            Users = allUsers
                .Select(u => new RealmUserListItem(u, editableIds.Contains(u.Id)))
                .OrderByDescending(item => item.IsEditable)
                .ThenBy(item => item.User.UserName)
                .ToArray();
        }

        return Page();
    }

    private static string DomainOf(ApplicationUser user)
    {
        var name = user?.UserName ?? user?.Email ?? "";
        int at = name.LastIndexOf('@');
        return at >= 0 && at < name.Length - 1 ? name.Substring(at + 1).ToLowerInvariant() : "";
    }

    public sealed record RealmUserListItem(ApplicationUser User, bool IsEditable);
}
