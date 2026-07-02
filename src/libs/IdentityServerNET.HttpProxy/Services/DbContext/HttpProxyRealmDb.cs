using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Distribution.Services;
using IdentityServerNET.Distribution.ValueTypes;
using IdentityServerNET.Models;

namespace IdentityServerNET.HttpProxy.Services.DbContext;

public class HttpProxyRealmDb : IRealmDbContext
{
    private readonly HttpInvokerService<IRealmDbContext> _httpInvoker;

    public HttpProxyRealmDb(HttpInvokerService<IRealmDbContext> httpInvoker)
    {
        _httpInvoker = httpInvoker;
    }

    public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<RealmModel?>(
                Helper.GetMethod<IRealmDbContext>(nameof(FindByNameAsync)),
                realmName);

    public Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<RealmModel?>(
                Helper.GetMethod<IRealmDbContext>(nameof(FindByDomainAsync)),
                domain);

    async public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IEnumerable<RealmModel>>(
                Helper.GetMethod<IRealmDbContext>(nameof(GetAllAsync))) ?? [];

    public Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, RealmModel>(
                Helper.GetMethod<IRealmDbContext>(nameof(CreateAsync)),
                realm);

    public Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, RealmModel>(
                Helper.GetMethod<IRealmDbContext>(nameof(UpdateAsync)),
                realm);

    public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, RealmModel>(
                Helper.GetMethod<IRealmDbContext>(nameof(DeleteAsync)),
                realm);
}
