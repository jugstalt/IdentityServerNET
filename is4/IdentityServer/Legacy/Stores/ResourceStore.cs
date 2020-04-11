using IdentityServer.Legacy.Services.DbContext;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        async public Task<ApiResource> FindApiResourceAsync(string name)
        {
            return await _resourcedbContext.FindApiResourceAsync(name);
        }

        async public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return await _resourcedbContext.FindApiResourcesByScopeAsync(scopeNames);
        }

        public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            List<IdentityResource> identityResources = new List<IdentityResource>();

            foreach (var scopeName in scopeNames)
            {
                switch (scopeName.ToLower())
                {
                    case "openid":
                        identityResources.Add(new IdentityResources.OpenId());
                        break;
                    case "profile":
                        identityResources.Add(new IdentityResources.Profile());
                        break;
                    case "email":
                        identityResources.Add(new IdentityResources.Email());
                        break;
                    case "address":
                        identityResources.Add(new IdentityResources.Address());
                        break;
                    case "phone":
                        identityResources.Add(new IdentityResources.Phone());
                        break;
                    case "role":
                        identityResources.Add(new IdentityResource("role", "Your Role(s)", new[] { IdentityModel.JwtClaimTypes.Role }));
                        break;
                }
            }

            return Task.FromResult<IEnumerable<IdentityResource>>(identityResources.ToArray());
        }

        public Task<Resources> GetAllResourcesAsync()
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

            return Task.FromResult(resources);
        }
    }
}
