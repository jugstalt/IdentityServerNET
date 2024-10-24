using IdentityServerNET.Abstractions.DbContext;
using System.Linq;

namespace IdentityServer.Areas.Admin.Pages.Clients;

public class IndexModel : AdminPageModel
{
    private IExportClientDbContext _exportClientDb = null;
    private IExportResourceDbContext _exportResourceDb = null;

    public IndexModel(IExportClientDbContext exportClientDb = null,
                      IExportResourceDbContext exportResourceDb = null)
    {
        _exportClientDb = exportClientDb;
        _exportResourceDb = exportResourceDb;
    }

    public bool HasExportClientDbContext => _exportClientDb != null;
    public string ExportClientDbContextType => _exportClientDb?.GetType().ToString().Split('.').Last();
    public bool HasExportResourceDbContext => _exportResourceDb != null;
    public string ExportResourceDbContextType => _exportResourceDb?.GetType().ToString().Split('.').Last();

    public bool HasExports => HasExportClientDbContext || HasExportResourceDbContext;

    public string ExportClientsMessage { get; set; }
    public string ExportResourcesMessage { get; set; }

    public void OnGet(string exportClientsMessage = null, string exportResourcesMessage = null)
    {
        ExportClientsMessage = exportClientsMessage;
        ExportResourcesMessage = exportResourcesMessage;
    }
}
