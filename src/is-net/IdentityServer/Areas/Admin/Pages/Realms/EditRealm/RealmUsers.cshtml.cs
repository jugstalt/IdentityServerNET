using IdentityServerNET.Abstractions.DbContext;
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

    public RealmUsersModel(IRealmDbContext realmDb, IUserDbContext userDb) : base(realmDb)
    {
        _userDb = userDb;
    }

    public IEnumerable<ApplicationUser> Users { get; private set; } = Array.Empty<ApplicationUser>();

    public async Task<IActionResult> OnGetAsync(string name)
    {
        await LoadCurrentRealmAsync(name);
        if (CurrentRealm == null) return NotFound();

        if (_userDb is IAdminUserDbContext adminUserDb)
        {
            var domains = new HashSet<string>(
                CurrentRealm.Domains ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var allUsers = await adminUserDb.GetUsersAsync(1000, 0, CancellationToken.None);
            Users = allUsers.Where(u => u != null && domains.Contains(DomainOf(u))).ToArray();
        }

        return Page();
    }

    private static string DomainOf(ApplicationUser user)
    {
        var name = user?.UserName ?? user?.Email ?? "";
        int at = name.LastIndexOf('@');
        return at >= 0 && at < name.Length - 1 ? name.Substring(at + 1).ToLowerInvariant() : "";
    }
}
