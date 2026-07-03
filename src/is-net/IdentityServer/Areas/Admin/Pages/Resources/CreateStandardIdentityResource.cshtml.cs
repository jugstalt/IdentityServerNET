using IdentityServer4.Models;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models.IdentityServerWrappers;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources;

public class CreateStandardIdentityResourceModel : AdminPageModel
{
    private IResourceDbContextModify _resourceDb = null;
    private readonly IRealmContext _realmContext;

    public CreateStandardIdentityResourceModel(IResourceDbContext clientDbContext, IRealmContext realmContext = null)
    {
        _resourceDb = clientDbContext as IResourceDbContextModify;
        _realmContext = realmContext;
    }

    async public Task<IActionResult> OnGetAsync(string name)
    {
        if (_resourceDb != null)
        {
            var realm = _realmContext is not null ? await _realmContext.GetCurrentRealmNameAsync() : null;
            if (realm is not null)
            {
                // Realm admins cannot create standard resources — they are globally provided.
                StatusMessage = "Error: Standard OIDC resources are globally available and do not need to be added to a realm.";
                return RedirectToPage("./Identities");
            }

            return await SecureHandlerAsync(async () =>
            {
                var nestedType = typeof(IdentityResources).GetNestedType(name);
                if (nestedType != null)
                {
                    var identityResource = (IdentityResource)Activator.CreateInstance(nestedType);
                    await _resourceDb.AddIdentityResourceAsync(new IdentityResourceModel(identityResource));
                }
            },
            onFinally: () => RedirectToPage("./Identities"),
            successMessage: $"Standard resource '{name}' added.",
            onException: _ => RedirectToPage("./Identities"));
        }

        return RedirectToPage("./Identities");
    }
}
