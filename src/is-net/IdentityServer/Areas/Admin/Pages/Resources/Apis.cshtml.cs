using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Models.IdentityServerWrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Resources;

public class ApisModel : AdminPageModel
{
    #region Default API Resources 

    private const string SecretsVaultApiName = "secrets-vault";
    private const string SigningApiName = "signing-api";

    private static NewApiResource SecretsVaultApi = new NewApiResource()
    {
        ApiResourceName = SecretsVaultApiName,
        ApiResourceDisplayName = "IdentityServer NET Secrets Vault API"
    };
    private static NewApiResource SigningApi = new NewApiResource()
    {
        ApiResourceName = SigningApiName,
        ApiResourceDisplayName = "IdentityServer NET (Payload) Signing API"
    };

    #endregion

    private IResourceDbContextModify _resourceDb = null;
    private IConfiguration _configuration;
    private IRealmContext _realmContext;
    public ApisModel(IResourceDbContext clientDbContext, IConfiguration configuration, IRealmContext realmContext)
    {
        _resourceDb = clientDbContext as IResourceDbContextModify;
        _configuration = configuration;
        _realmContext = realmContext;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        if (_resourceDb != null)
        {
            // GetAllApiResources is the shared runtime read (used by IdentityServer), so the list is
            // scoped to the current realm here, at the admin page, rather than in the store decorator.
            var realm = await _realmContext.GetCurrentRealmNameAsync();
            this.ApiResources = (await _resourceDb.GetAllApiResources())
                .Where(r => r.Name.BelongsToRealm(realm))
                .ToArray();

            // The built-in system APIs are only offered to the system admin (global namespace).
            if (realm is null)
            {
                if (!_configuration.DenyAdminSecretsVault()
                   && !this.ApiResources.Any(r => r.Name == SecretsVaultApiName))
                {
                    DefaultApiResources.Add(SecretsVaultApi);
                }

                if (!_configuration.DenySigningUI()
                   && !this.ApiResources.Any(r => r.Name == SigningApiName))
                {
                    DefaultApiResources.Add(SigningApi);
                }
            }

            Input = new NewApiResource();
        }

        return Page();
    }

    public Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Task.FromResult<IActionResult>(Page());
        }

        return CreateApiResource(
                Input.ApiResourceName.Trim().ToLower(),
                Input.ApiResourceDisplayName?.Trim()
            );
    }

    public Task<IActionResult> OnGetAddAsync(string name, string displayName)
        => CreateApiResource(name, displayName);

    public IEnumerable<ApiResourceModel> ApiResources { get; set; }

    public List<NewApiResource> DefaultApiResources { get; set; } = new();

    [BindProperty]
    public NewApiResource Input { get; set; }

    public class NewApiResource
    {
        [Required, MinLength(3), RegularExpression(@"^[a-z0-9_\-\.]+$", ErrorMessage = "Only lowercase letters, numbers,-,_,.")]
        public string ApiResourceName { get; set; }
        public string ApiResourceDisplayName { get; set; }
    }

    private Task<IActionResult> CreateApiResource(
            string apiName,
            string displayName
        )
    {
        string redirectId = apiName;

        return SecureHandlerAsync(async () =>
    {
        if (_resourceDb != null)
        {
            if (string.IsNullOrEmpty(apiName))
            {
                new StatusMessageException("Invalid API name");
            }

            var apiResource = new ApiResourceModel(apiName, displayName)
            {
                Scopes = apiName switch
                {
                    SecretsVaultApiName => [new ScopeModel() { Name = apiName }],
                    SigningApiName => [new ScopeModel() { Name = apiName }],
                    _ => [
                            new ScopeModel() { Name = apiName },
                            new ScopeModel() { Name = $"{apiName}.query" },
                            new ScopeModel() { Name = $"{apiName}.command" }
                            ]
                }
            };

            await _resourceDb.AddApiResourceAsync(apiResource);

            // The realm-scoping decorator may have appended @realm to the name in place.
            redirectId = apiResource.Name;
        }
    }
    , onFinally: () => RedirectToPage("EditApi/Index", new { id = redirectId })
    , successMessage: "API resource successfully created"
    , onException: (ex) => RedirectToPage());
    }
}
