using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Humanizer;

namespace IdentityServerNET;

class ResourceStore : IResourceStore
{
    public ResourceStore(IResourceDbContext resourceDbContext)
    {
        _resourcedbContext = resourceDbContext;
    }

    private IResourceDbContext _resourcedbContext = null;

    //
    // Summary:
    //     Gets API resources by scope name.
    //Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames);
    //
    // Summary:
    //     Gets API scopes by scope name.
    //Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames);



    async public Task<ApiResource> FindApiResourceByNameAsync(string name)
    {
        return (await _resourcedbContext.FindApiResourceAsync(name)).IndentityServer4Instance as ApiResource;
    }

    async public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames)
    {
        return (await _resourcedbContext.GetAllApiResources())
                                        .Where(r => apiResourceNames.Contains(r.Name))
                                        .Select(r => (ApiResource)r.IndentityServer4Instance);
    }

    async public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames)
    {
        List<ScopeModel> scopes = new List<ScopeModel>();

        scopes.AddRange((await _resourcedbContext.GetAllApiResources())
                                                 .Where(r => r.Scopes != null)
                                                 .SelectMany(r => r.Scopes));

        return scopes.Where(s => scopeNames.Contains(s.Name))
                     .Select(s => s.IdentitityServer4Insance);
    }

    async public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        return (await _resourcedbContext.FindApiResourcesByScopeAsync(scopeNames))
                    .Select(r => (ApiResource)r.IndentityServer4Instance);
    }

    async public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames)
    {
        List<IdentityResource> identityResources = new List<IdentityResource>();

        foreach (var scopeName in scopeNames)
        {
            var identityResource = (await _resourcedbContext.FindIdentityResource(scopeName))?.IndentityServer4Instance as IdentityResource;
            if (identityResource is not null)
            {
                identityResources.Add(identityResource.Name.ToLower() switch
                {
                    "openid" => new IdentityResources.OpenId(),
                    "profile" => new IdentityResources.Profile(),
                    "email" => new IdentityResources.Email(),
                    "address" => new IdentityResources.Address(),
                    "phone" => new IdentityResources.Phone(),
                    "role" => new IdentityResources.Role(),
                    _ => identityResource
                });
            }
        }

        return identityResources;
    }

    async public Task<Resources> GetAllResourcesAsync()
    {
        var resources = new Resources();

        //resources.ApiResources = new ApiResource[]
        //{
        //    await this.FindApiResourceAsync("api1")
        //};

        resources.IdentityResources = new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new IdentityResources.Email(),
            new IdentityResources.Address(),
            new IdentityResources.Phone(),
            new IdentityResources.Role()
        };

        if (_resourcedbContext is IResourceDbContextModify)
        {
            resources.ApiResources = (await ((IResourceDbContextModify)_resourcedbContext).GetAllApiResources())
                                        .Select(r => (ApiResource)r.IndentityServer4Instance)
                                        .ToArray();
        }

        return resources;
    }
}
