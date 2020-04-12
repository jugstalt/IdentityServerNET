using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class ExportResourceDbModel : AdminPageModel
    {
        private IResourceDbContextModify _resourcetDb = null;
        private IExportResourceDbContext _exportResourceDb = null;

        public ExportResourceDbModel(
            IResourceDbContext clientDbContext,
            IExportResourceDbContext exportClientDbContext)
        {
            _resourcetDb = clientDbContext as IResourceDbContextModify;
            _exportResourceDb = exportClientDbContext;
        }

        async public Task<IActionResult> OnGetAsync()
        {
            var apiResources = await _resourcetDb.GetAllApiResources();
            var identityResources = await _resourcetDb.GetAllIdentityResources();
            
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

                    foreach(var indentityResource in identityResources)
                    {
                        await _exportResourceDb.AddIdentityResourceAsync(indentityResource);
                    }

                    msg = $"Flushed target Db and exported { count } resources";
                }
                else
                {
                    msg = "Nothing to export. Target Db untouched";
                }
            }
            catch (Exception ex)
            {
                msg = $"Exception: { ex.Message }";
            }

            return RedirectToPage("./Index", new { exportResourcesMessage = msg });
        }
    }
}
