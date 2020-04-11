using IdentityServer.Legacy.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class InMemoryResourceDb : IResourceDbContextModify
    {
        private static ConcurrentDictionary<string, ApiResource> _apiResources = null;

        public InMemoryResourceDb(IOptions<ResourceDbContextConfiguration> options = null)
        {
            if (_apiResources == null)
            {
                _apiResources = new ConcurrentDictionary<string, ApiResource>();

                // Init from configuration
                if (options?.Value?.IntialApiResources != null)
                {
                    foreach (var resource in options.Value.IntialApiResources)
                    {
                        _apiResources[resource.Name] = resource;
                    }
                }
            }
        }

        #region IResourceDbContext

        public Task<ApiResource> FindApiResourceAsync(string name)
        {
            if (_apiResources.ContainsKey(name))
            {
                return Task.FromResult(_apiResources[name]);
            }

            return Task.FromResult<ApiResource>(null);
        }

        public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult<IEnumerable<ApiResource>>(
                _apiResources.Values.Where(r => scopeNames.Contains(r.Name)));
        }

        public Task AddApiResourceAsync(ApiResource apiResource)
        {
            if(_apiResources.ContainsKey(apiResource.Name))
            {
                throw new Exception($"Api with clientId { apiResource.Name } already exists");
            }

            _apiResources[apiResource.Name] = apiResource;

            return Task.FromResult(0);
        }

        public Task UpdateApiResourceAsync(ApiResource resource)
        {
            if (!_apiResources.ContainsKey(resource.Name))
            {
                throw new Exception($"Api with clientId { resource.Name } not exists");
            }

            _apiResources[resource.Name] = resource;

            return Task.FromResult(0);
        }

        public Task RemoveApiResourceAsync(ApiResource apiResource)
        {
            if (!_apiResources.ContainsKey(apiResource.Name))
            {
                throw new Exception($"Api with clientId { apiResource.Name } not exists");
            }

            if(!_apiResources.TryRemove(apiResource.Name, out apiResource))
            {
                throw new Exception($"Can't remove api");
            }

            return Task.FromResult(0);
        }

        public Task<IEnumerable<ApiResource>> GetAllApiResources()
        {
            return Task.FromResult<IEnumerable<ApiResource>>(_apiResources.Values.ToArray());
        }

        #endregion
    }
}
