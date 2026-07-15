using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients;

public class ExportClientDbModel : AdminPageModel
{
    private IClientDbContextModify _clientDb = null;
    private IExportClientDbContext _exportClientDb = null;
    private readonly IRealmContext _realmContext = null;

    public ExportClientDbModel(
        IClientDbContext clientDbContext,
        IExportClientDbContext exportClientDbContext,
        IRealmContext realmContext = null)
    {
        _clientDb = clientDbContext as IClientDbContextModify;
        _exportClientDb = exportClientDbContext;
        _realmContext = realmContext;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        // Belt-and-suspenders: IClientDbContext is always resolved to RealmScopedClientDbContext,
        // whose own GetAllClients() already filters to the caller's realm (see that class - a
        // realm admin never sees another realm's clients, and a system admin only ever sees
        // non-namespaced/global clients, never realm-namespaced ones, through that decorator).
        // Filtering again here costs nothing and keeps this page's intent explicit and independent
        // of that decorator's behavior, matching the same pattern already used by DataTransfer/Index.
        var realmName = _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();
        var allClients = await _clientDb.GetAllClients();
        var clients = realmName is null
            ? allClients
            : allClients.Where(c => c.ClientId.BelongsToRealm(realmName));

        var count = clients.Count();
        string msg = String.Empty;

        try
        {
            if (count > 0)
            {
                await _exportClientDb.FlushDb();

                foreach (var client in clients)
                {
                    await _exportClientDb.AddClientAsync(client);
                }

                msg = $"Flushed target Db and exported {count} clients";
            }
            else
            {
                msg = "Nothing to export. Target Db untouched";
            }
        }
        catch (Exception ex)
        {
            msg = $"Exception: {ex.Message}";
        }

        return RedirectToPage("./Index", new { exportClientsMessage = msg });
    }
}
