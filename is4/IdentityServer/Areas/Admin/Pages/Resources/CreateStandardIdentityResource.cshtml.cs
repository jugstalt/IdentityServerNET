using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources;

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
                await _resourceDb.AddIdentityResourceAsync(new IdentityResourceModel(identityResource));
            }
        }

        return RedirectToPage("./Identities");
    }
}
