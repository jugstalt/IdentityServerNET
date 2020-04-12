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
        private static ConcurrentDictionary<string, IdentityResource> _identityResources = null;

        public InMemoryResourceDb(IOptions<ResourceDbContextConfiguration> options = null)
        {
            if (_apiResources == null)
            {
                _apiResources = new ConcurrentDictionary<string, ApiResource>();

                // Init from configuration
                if (options?.Value?.InitialApiResources != null)
                {
                    foreach (var resource in options.Value.InitialApiResources)
                    {
                        _apiResources[resource.Name] = resource;
                    }
                }
            }

            if(_identityResources==null)
            {
                _identityResources = new ConcurrentDictionary<string, IdentityResource>();

                // Init from configuration
                if (options?.Value?.InitialIdentityResources != null)
                {
                    foreach (var resource in options.Value.InitialIdentityResources)
                    {
                        _identityResources[resource.Name] = resource;
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
                throw new Exception($"Api { apiResource.Name } already exists");
            }

            _apiResources[apiResource.Name] = apiResource;

            return Task.CompletedTask;
        }

        public Task UpdateApiResourceAsync(ApiResource resource)
        {
            if (!_apiResources.ContainsKey(resource.Name))
            {
                throw new Exception($"Api { resource.Name } not exists");
            }

            _apiResources[resource.Name] = resource;

            return Task.CompletedTask;
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

            return Task.CompletedTask;
        }

        public Task<IEnumerable<ApiResource>> GetAllApiResources()
        {
            return Task.FromResult<IEnumerable<ApiResource>>(_apiResources.Values);
        }

        public Task<IdentityResource> FindIdentityResource(string name)
        {
            if(_identityResources.ContainsKey(name))
            {
                return Task.FromResult(_identityResources[name]);
            }

            return Task.FromResult<IdentityResource>(null);
        }

        public Task<IEnumerable<IdentityResource>> GetAllIdentityResources()
        {
            return Task.FromResult<IEnumerable<IdentityResource>>(_identityResources.Values);
        }

        public Task AddIdentityResourceAsync(IdentityResource identityResource)
        {
            if (_identityResources.ContainsKey(identityResource.Name))
            {
                throw new Exception($"Identity resource { identityResource.Name } already exists");
            }

            _identityResources[identityResource.Name] = identityResource;

            return Task.CompletedTask;
        }

        public Task UpdateIdentityResourceAsync(IdentityResource identityResource)
        {
            if (!_identityResources.ContainsKey(identityResource.Name))
            {
                throw new Exception($"Api with clientId { identityResource.Name } not exists");
            }

            _identityResources[identityResource.Name] = identityResource;

            return Task.CompletedTask;
        }

        public Task RemoveIdentityResourceAsync(IdentityResource identityResource)
        {
            if (!_identityResources.ContainsKey(identityResource.Name))
            {
                throw new Exception($"Identity { identityResource.Name } not exists");
            }

            if (!_identityResources.TryRemove(identityResource.Name, out identityResource))
            {
                throw new Exception($"Can't remove identity");
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
