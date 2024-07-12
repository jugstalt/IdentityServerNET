using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients;

public class ExportClientDbModel : AdminPageModel
{
    private IClientDbContextModify _clientDb = null;
    private IExportClientDbContext _exportClientDb = null;

    public ExportClientDbModel(
        IClientDbContext clientDbContext,
        IExportClientDbContext exportClientDbContext)
    {
        _clientDb = clientDbContext as IClientDbContextModify;
        _exportClientDb = exportClientDbContext;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        var clients = await _clientDb.GetAllClients();
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
