using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources;

public class IdentitiesModel : AdminPageModel
{
    private IResourceDbContextModify _resourceDb = null;
    private IRealmContext _realmContext;
    public IdentitiesModel(IResourceDbContext clientDbContext, IRealmContext realmContext)
    {
        _resourceDb = clientDbContext as IResourceDbContextModify;
        _realmContext = realmContext;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        if (_resourceDb != null)
        {
            CurrentRealm = await _realmContext.GetCurrentRealmNameAsync();
            this.IdentityResources = (await _resourceDb.GetAllIdentityResources())
                .Where(r => r.Name.BelongsToRealm(CurrentRealm))
                .ToArray();

            // For the "Add Standard" section (system admin only): track all existing names
            // so we can suppress already-present resources from the add list.
            AllIdentityResourceNames = CurrentRealm is null
                ? this.IdentityResources.Select(r => r.Name.ToLower()).ToHashSet()
                : null; // realm admins don't see this section at all

            Input = new NewIdentityResource();
        }

        return Page();
    }

    async public Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        string identityName = Input.IdentityResourceName.Trim().ToLower();

        return await SecureHandlerAsync(async () =>
        {
            if (_resourceDb != null)
            {
                var identityResource = new IdentityResourceModel()
                {
                    Name = Input.IdentityResourceName,
                    DisplayName = Input.IdentityResourceDisplayName
                };

                await _resourceDb.AddIdentityResourceAsync(identityResource);

                // The realm-scoping decorator may have appended @realm to the name in place.
                identityName = identityResource.Name;
            }
        }
        , onFinally: () => RedirectToPage("EditIdentity/Index", new { id = identityName })
        , successMessage: "Identity resource successfully created"
        , onException: (ex) => RedirectToPage());
    }

    public IEnumerable<IdentityResourceModel> IdentityResources { get; set; }
    public string CurrentRealm { get; private set; }
    public HashSet<string> AllIdentityResourceNames { get; private set; }

    [BindProperty]
    public NewIdentityResource Input { get; set; }

    public class NewIdentityResource
    {
        [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
        public string IdentityResourceName { get; set; }
        public string IdentityResourceDisplayName { get; set; }
    }
}
