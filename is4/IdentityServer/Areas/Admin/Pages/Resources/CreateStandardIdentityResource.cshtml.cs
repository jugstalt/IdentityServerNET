using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class CreateStandardIdentityResourceModel : AdminPageModel
    {
        private IResourceDbContextModify _resourceDb = null;
        public CreateStandardIdentityResourceModel(IResourceDbContext clientDbContext)
        {
            _resourceDb = clientDbContext as IResourceDbContextModify;
        }

        async public Task<IActionResult> OnGetAsync(string name)
        {
            if (_resourceDb != null)
            {
                var nestedType = typeof(IdentityResources).GetNestedType(name);
                if (nestedType != null)
                {
                    var identityResource = (IdentityResource)Activator.CreateInstance(nestedType);
                    await _resourceDb.AddIdentityResourceAsync(identityResource);
                }
            }

            return RedirectToPage("./Identities");
        }
    }
}
