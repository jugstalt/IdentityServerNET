using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Distribution.ValueTypes;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.HttpProxy.Services.DbContext;

public class HttpProxyResourceDb : IResourceDbContextModify
{
    private readonly HttpInvokerService<IResourceDbContextModify> _httpInvoker;

    public HttpProxyResourceDb(
            HttpInvokerService<IResourceDbContextModify> httpInvoker)
    {
        _httpInvoker = httpInvoker;
    }

    #region IResouceDbContext

    public Task<ApiResourceModel?> FindApiResourceAsync(string name)
        => _httpInvoker.HandleGetAsync<ApiResourceModel?>(
                Helper.GetMethod<IResourceDbContext>(nameof(FindApiResourceAsync)),
                name);

    async public Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApiResourceModel>>(
                Helper.GetMethod<IResourceDbContext>(nameof(FindApiResourcesByScopeAsync)),
                scopeNames) ?? [];
    async public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApiResourceModel>>(
                Helper.GetMethod<IResourceDbContext>(nameof(GetAllApiResources))) ?? [];

    public Task<IdentityResourceModel?> FindIdentityResource(string name)
        => _httpInvoker.HandleGetAsync<IdentityResourceModel?>(
                Helper.GetMethod<IResourceDbContext>(nameof(FindIdentityResource)),
                name);
    async public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
        => await _httpInvoker.HandleGetAsync<IEnumerable<IdentityResourceModel>>(
                Helper.GetMethod<IResourceDbContext>(nameof(GetAllIdentityResources))) ?? [];

    #endregion

    #region IResourceDbContextModify

    public Task AddApiResourceAsync(ApiResourceModel apiResource)
        => _httpInvoker.HandlePostAsync<NoResult, ApiResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(AddApiResourceAsync)),
                apiResource);

    public Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string>? propertyNames = null)
        => _httpInvoker.HandlePostAsync<NoResult, ApiResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(UpdateApiResourceAsync)),
                apiResource, propertyNames ?? []);

    public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
        => _httpInvoker.HandlePostAsync<NoResult, ApiResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(RemoveApiResourceAsync)),
                apiResource);

    public Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
        => _httpInvoker.HandlePostAsync<NoResult, IdentityResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(AddIdentityResourceAsync)),
                identityResource);

    public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string>? propertyNames = null)
        => _httpInvoker.HandlePostAsync<NoResult, IdentityResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(UpdateIdentityResourceAsync)),
                identityResource, propertyNames ?? []);

    public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
        => _httpInvoker.HandlePostAsync<NoResult, IdentityResourceModel>(
                Helper.GetMethod<IResourceDbContextModify>(nameof(RemoveIdentityResourceAsync)),
                identityResource);

    #endregion
}
