using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy
{
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
                //switch (scopeName.ToLower())
                //{
                //    case "openid":
                //        identityResources.Add(new IdentityResources.OpenId());
                //        break;
                //    case "profile":
                //        identityResources.Add(new IdentityResources.Profile());
                //        break;
                //    case "email":
                //        identityResources.Add(new IdentityResources.Email());
                //        break;
                //    case "address":
                //        identityResources.Add(new IdentityResources.Address());
                //        break;
                //    case "phone":
                //        identityResources.Add(new IdentityResources.Phone());
                //        break;
                //    case "role":
                //        identityResources.Add(new IdentityResource("role", "Your Role(s)", new[] { IdentityModel.JwtClaimTypes.Role }));
                //        break;
                //}

                var identityResource = (await _resourcedbContext.FindIdentityResource(scopeName))?.IndentityServer4Instance as IdentityResource;
                if (identityResource != null)
                {
                    if (identityResource.Name == "role")
                    {
                        identityResource = new IdentityResource("role", "Your Role(s)", new[] { IdentityModel.JwtClaimTypes.Role });
                    }
                    identityResources.Add(identityResource);
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
                //new IdentityResources.Email(),
                //new IdentityResources.Address(),
                //new IdentityResources.Phone(),
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
}
