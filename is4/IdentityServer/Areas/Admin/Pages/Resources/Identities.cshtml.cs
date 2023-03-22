using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources
{
    public class IdentitiesModel : AdminPageModel
    {
        private IResourceDbContextModify _resourceDb = null;
        public IdentitiesModel(IResourceDbContext clientDbContext)
        {
            _resourceDb = clientDbContext as IResourceDbContextModify;
        }

        async public Task<IActionResult> OnGetAsync()
        {
            if (_resourceDb != null)
            {
                this.IdentityResources = await _resourceDb.GetAllIdentityResources();

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
                }
            }
            , onFinally: () => RedirectToPage("EditIdentity/Index", new { id = identityName })
            , successMessage: "Identity resource successfully created"
            , onException: (ex) => RedirectToPage());
        }

        public IEnumerable<IdentityResourceModel> IdentityResources { get; set; }

        [BindProperty]
        public NewIdentityResource Input { get; set; }

        public class NewIdentityResource
        {
            [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
            public string IdentityResourceName { get; set; }
            public string IdentityResourceDisplayName { get; set; }
        }
    }
}
