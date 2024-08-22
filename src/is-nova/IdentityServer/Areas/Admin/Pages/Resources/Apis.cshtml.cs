using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Exceptions;
using IdentityServer.Nova.Extensions;
using IdentityServer.Nova.Models.IdentityServerWrappers;
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
        ApiResourceDisplayName = "IdentityServer.Nova Secrets Vault API"
    };
    private static NewApiResource SigningApi = new NewApiResource()
    {
        ApiResourceName = SigningApiName,
        ApiResourceDisplayName = "IdentityServer.Nova (Payload) Signing API"
    };

    #endregion

    private IResourceDbContextModify _resourceDb = null;
    private IConfiguration _configuration;
    public ApisModel(IResourceDbContext clientDbContext, IConfiguration configuration)
    {
        _resourceDb = clientDbContext as IResourceDbContextModify;
        _configuration = configuration;
    }

    async public Task<IActionResult> OnGetAsync()
    {
        if (_resourceDb != null)
        {
            this.ApiResources = await _resourceDb.GetAllApiResources();

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
        ) => SecureHandlerAsync(async () =>
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
        }
    }
    , onFinally: () => RedirectToPage("EditApi/Index", new { id = apiName })
    , successMessage: "API resource successfully created"
    , onException: (ex) => RedirectToPage());
}
