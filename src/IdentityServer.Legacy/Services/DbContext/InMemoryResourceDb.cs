using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class InMemoryResourceDb : IResourceDbContextModify
    {
        private static ConcurrentDictionary<string, ApiResourceModel> _apiResources = null;
        private static ConcurrentDictionary<string, IdentityResourceModel> _identityResources = null;

        public InMemoryResourceDb(IOptions<ResourceDbContextConfiguration> options = null)
        {
            if (_apiResources == null)
            {
                _apiResources = new ConcurrentDictionary<string, ApiResourceModel>();

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
                _identityResources = new ConcurrentDictionary<string, IdentityResourceModel>();

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

        public Task<ApiResourceModel> FindApiResourceAsync(string name)
        {
            if (_apiResources.ContainsKey(name))
            {
                return Task.FromResult(_apiResources[name]);
            }

            return Task.FromResult<ApiResourceModel>(null);
        }

        public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        {
            return Task.FromResult<IEnumerable<ApiResourceModel>>(
                _apiResources.Values.Where(r => scopeNames.Contains(r.Name)));
        }

        public Task AddApiResourceAsync(ApiResourceModel apiResource)
        {
            if(_apiResources.ContainsKey(apiResource.Name))
            {
                throw new Exception($"Api { apiResource.Name } already exists");
            }

            _apiResources[apiResource.Name] = apiResource;

            return Task.CompletedTask;
        }

        public Task UpdateApiResourceAsync(ApiResourceModel resource, IEnumerable<string> propertyNames = null)
        {
            if (!_apiResources.ContainsKey(resource.Name))
            {
                throw new Exception($"Api { resource.Name } not exists");
            }

            _apiResources[resource.Name] = resource;

            return Task.CompletedTask;
        }

        public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
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

        public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
        {
            return Task.FromResult<IEnumerable<ApiResourceModel>>(_apiResources.Values);
        }

        public Task<IdentityResourceModel> FindIdentityResource(string name)
        {
            if(_identityResources.ContainsKey(name))
            {
                return Task.FromResult(_identityResources[name]);
            }

            return Task.FromResult<IdentityResourceModel>(null);
        }

        public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
        {
            return Task.FromResult<IEnumerable<IdentityResourceModel>>(_identityResources.Values);
        }

        public Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
        {
            if (_identityResources.ContainsKey(identityResource.Name))
            {
                throw new Exception($"Identity resource { identityResource.Name } already exists");
            }

            _identityResources[identityResource.Name] = identityResource;

            return Task.CompletedTask;
        }

        public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string> propertyNames)
        {
            if (!_identityResources.ContainsKey(identityResource.Name))
            {
                throw new Exception($"Api with clientId { identityResource.Name } not exists");
            }

            _identityResources[identityResource.Name] = identityResource;

            return Task.CompletedTask;
        }

        public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
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
