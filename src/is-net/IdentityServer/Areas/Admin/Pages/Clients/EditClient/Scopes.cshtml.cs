using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin.Pages.Clients.EditClient;

public class ScopesModel : EditClientPageModel
{
    public ScopesModel(IClientDbContext clientDbContext, IResourceDbContext resourceDbContext, IRealmContext realmContext)
         : base(clientDbContext)
    {
        _resourceDb = resourceDbContext as IResourceDbContextModify;
        _realmContext = realmContext;
    }

    private IResourceDbContextModify _resourceDb = null;
    private IRealmContext _realmContext;

    // Identity resources: a client may be assigned scopes from resources of its own realm plus the
    // global (system) ones — the latter carry the standard OIDC scopes (openid, profile, ...) every
    // realm needs, so they stay visible to everyone by design.
    private static bool IsAssignable(string resourceName, string realm)
        => resourceName.BelongsToRealm(realm) || !resourceName.HasRealm();

    // API resources: unlike identity resources, a custom API resource typically represents one specific
    // backend service, not something every realm's clients need — a realm admin should not automatically
    // see (and be able to grant a client) every other tenant's or the system's own custom APIs. Only the
    // caller's own realm's API resources, plus the small set of genuinely shared system resources, are
    // assignable.
    private static readonly string[] SharedSystemApiResourceNames = { "secrets-vault" };

    private static bool IsApiResourceAssignable(string resourceName, string realm)
        => resourceName.BelongsToRealm(realm)
           || (realm is not null && SharedSystemApiResourceNames.Contains(resourceName));

    // Unlike other shared APIs, individual scopes on the global "secrets-vault" resource each guard a
    // different tenant's private locker (e.g. "secrets-vault.my-locker@acme") — the resource itself has
    // no realm, but a specific scope on it does. Only the caller's own realm's locker scopes (or, for
    // the system admin, un-suffixed/global ones) may be assigned to a client. The base scope (named
    // exactly like the resource, "secrets-vault") is the general "may call this API at all" gate, not
    // tied to any locker/tenant, so it stays visible to everyone — a client needs it alongside its own
    // locker scope to retrieve secrets.
    private static bool IsScopeAssignable(string resourceName, string scopeName, string realm)
        => resourceName != "secrets-vault" || scopeName == resourceName || scopeName.BelongsToRealm(realm);

    public string[] IdentityResourceScopes = null;
    public string[] ApiResouceScopes = null;

    async public Task<IActionResult> OnGetAsync(string id)
    {
        await LoadCurrentClientAsync(id);

        List<ResourceScope> resourceScopes = new List<ResourceScope>();
        if (_resourceDb != null)
        {
            var realm = await _realmContext.GetCurrentRealmNameAsync();

            var identityResources = this.CurrentClient.AllowedGrantTypes.Contains("authorization_code")
                ? (await _resourceDb.GetAllIdentityResources()).Where(r => IsAssignable(r.Name, realm)).ToArray()
                : null;

            var apiResources = this.CurrentClient.AllowedGrantTypes.Contains("client_credentials")
                ? (await _resourceDb.GetAllApiResources()).Where(r => IsApiResourceAssignable(r.Name, realm)).ToArray()
                : null;

            IdentityResourceScopes = identityResources?
                .Select(s => s.Name)
                .ToArray() ?? new string[0];
            ApiResouceScopes = apiResources?
                .Where(m => m.Scopes != null)
                .SelectMany(m => m.Scopes.Where(s => IsScopeAssignable(m.Name, s.Name, realm)).Select(s => s.Name))
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
                        .Where(s => IsScopeAssignable(apiResource.Name, s.Name, realm))
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
                await EnsureScopeGrantAllowedAsync(scopeName);

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
                await EnsureScopeGrantAllowedAsync(Input.ScopeName);

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

    // Defense in depth: reject a "secrets-vault.*" scope for another realm's (or the system's) locker
    // even if it was submitted directly (crafted URL/form) rather than picked from the filtered list.
    private async Task EnsureScopeGrantAllowedAsync(string scopeName)
    {
        if (scopeName.StartsWith("secrets-vault.", StringComparison.OrdinalIgnoreCase))
        {
            var realm = await _realmContext.GetCurrentRealmNameAsync();
            if (!scopeName.BelongsToRealm(realm))
            {
                throw new StatusMessageException($"Scope '{scopeName}' does not belong to your realm.");
            }
        }
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
