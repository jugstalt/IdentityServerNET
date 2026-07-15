using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources;

public class ExportResourceDbModel : AdminPageModel
{
    private IResourceDbContextModify _resourcetDb = null;
    private IExportResourceDbContext _exportResourceDb = null;
    private readonly IRealmContext _realmContext = null;

    public ExportResourceDbModel(
        IResourceDbContext clientDbContext,
        IExportResourceDbContext exportClientDbContext,
        IRealmContext realmContext = null)
    {
        _resourcetDb = clientDbContext as IResourceDbContextModify;
        _exportResourceDb = exportClientDbContext;
        _realmContext = realmContext;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        // Realm admins may only export their own realm's resources - a system admin (realmName ==
        // null) exports everything, matching the pattern already used by DataTransfer/Index.
        var realmName = _realmContext is null ? null : await _realmContext.GetCurrentRealmNameAsync();

        var allApiResources = await _resourcetDb.GetAllApiResources();
        var allIdentityResources = await _resourcetDb.GetAllIdentityResources();

        var apiResources = realmName is null
            ? allApiResources
            : allApiResources.Where(a => a.Name.BelongsToRealm(realmName));
        var identityResources = realmName is null
            ? allIdentityResources
            : allIdentityResources.Where(i => i.Name.BelongsToRealm(realmName));

        var count = apiResources.Count() + identityResources.Count();

        string msg = String.Empty;

        try
        {
            if (count > 0)
            {
                await _exportResourceDb.FlushDb();

                foreach (var apiResource in apiResources)
                {
                    await _exportResourceDb.AddApiResourceAsync(apiResource);
                }

                foreach (var indentityResource in identityResources)
                {
                    await _exportResourceDb.AddIdentityResourceAsync(indentityResource);
                }

                msg = $"Flushed target Db and exported {count} resources";
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

        return RedirectToPage("./Index", new { exportResourcesMessage = msg });
    }
}
