using IdentityServerNET.Abstractions.DbContext;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class ScopesModel : EditClientPageModel
{
    public ScopesModel(IClientDbContext clientDbContext, IResourceDbContext resourceDbContext)
         : base(clientDbContext)
    {
        _resourceDb = resourceDbContext as IResourceDbContextModify;
    }

    private IResourceDbContextModify _resourceDb = null;

    public string[] IdentityResourceScopes = null;
    public string[] ApiResouceScopes = null;

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentClientAsync(id);

        List<ResourceScope> resourceScopes = new List<ResourceScope>();
        if (_resourceDb != null)
        {
            var identityResources = this.CurrentClient.AllowedGrantTypes.Contains("authorization_code")
                ? await _resourceDb.GetAllIdentityResources()
                : null;

            var apiResources = this.CurrentClient.AllowedGrantTypes.Contains("client_credentials")
                ? await _resourceDb.GetAllApiResources()
                : null;

            IdentityResourceScopes = identityResources?
                .Select(s => s.Name)
                .ToArray() ?? new string[0];
            ApiResouceScopes = apiResources?
                .Where(m => m.Scopes != null)
                .SelectMany(m => m.Scopes.Select(s => s.Name))
                .ToArray() ?? new string[0];

            if (identityResources != null)
            {
                resourceScopes.AddRange(identityResources
                    .Where(i => this.CurrentClient.AllowedScopes == null || !this.CurrentClient.AllowedScopes.Contains(i.Name))
                    .Select(i =>
                           new ResourceScope()
                           {
                               ResourceType = "Identity",
                               Name = i.Name,
                               DisplayName = i.DisplayName
                           }));
            }
            if (apiResources != null)
            {
                foreach (var apiResource in apiResources.Where(a => a.Scopes != null))
                {
                    resourceScopes.AddRange(apiResource.Scopes
                        .Where(s => this.CurrentClient.AllowedScopes == null || !this.CurrentClient.AllowedScopes.Contains(s.Name))
                        .Select(s =>
                            new ResourceScope()
                            {
                                ResourceType = "API",
                                Name = s.Name,
                                DisplayName = s.DisplayName
                            }));
                }
            }
        }


        PossibleResourceScopes = resourceScopes.Count() > 0 ? resourceScopes : null;

        this.Input = new NewScopeModel()
        {
            ClientId = CurrentClient.ClientId
        };

        return Page();
    }

    async public Task<IActionResult> OnGetRemoveAsync(string id, string scopeName)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(id);

            this.CurrentClient.AllowedScopes = this.CurrentClient
                                                    .AllowedScopes
                                                    .Where(s => s != scopeName)
                                                    .ToArray();

            await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "AllowedScopes" });
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , $"Successfully removed scope {scopeName}");
    }

    async public Task<IActionResult> OnGetAddAsync(string id, string scopeName)
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(id);

            if (!String.IsNullOrWhiteSpace(scopeName))
            {
                List<string> allowedScopes = new List<string>();
                if (this.CurrentClient.AllowedScopes != null)
                {
                    allowedScopes.AddRange(this.CurrentClient.AllowedScopes);
                }

                if (!allowedScopes.Contains(scopeName.ToLower()))
                {
                    allowedScopes.Add(scopeName.ToLower());
                    this.CurrentClient.AllowedScopes = allowedScopes.ToArray();

                    await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "AllowedScopes" });
                }
            }
        }
        , onFinally: () => RedirectToPage(new { id = id })
        , successMessage: $"Scope '{scopeName}' addes successfully");
    }


    async public Task<IActionResult> OnPostAsync()
    {
        return await SecureHandlerAsync(async () =>
        {
            await LoadCurrentClientAsync(Input.ClientId);

            if (!String.IsNullOrWhiteSpace(Input.ScopeName))
            {
                List<string> allowedScopes = new List<string>();
                if (this.CurrentClient.AllowedScopes != null)
                {
                    allowedScopes.AddRange(this.CurrentClient.AllowedScopes);
                }

                if (!allowedScopes.Contains(Input.ScopeName.ToLower()))
                {
                    allowedScopes.Add(Input.ScopeName.ToLower());
                    this.CurrentClient.AllowedScopes = allowedScopes.ToArray();

                    await _clientDb.UpdateClientAsync(this.CurrentClient, new[] { "AllowedScopes" });
                }
            }
        }
        , onFinally: () => RedirectToPage(new { id = Input.ClientId })
        , successMessage: $"Scope '{Input.ScopeName}' addes successfully");
    }

    [BindProperty]
    public NewScopeModel Input { get; set; }

    public IEnumerable<ResourceScope> PossibleResourceScopes { get; set; }

    public class NewScopeModel
    {
        public string ClientId { get; set; }
        public string ScopeName { get; set; }
    }

    public class ResourceScope
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ResourceType { get; set; }
    }
}
