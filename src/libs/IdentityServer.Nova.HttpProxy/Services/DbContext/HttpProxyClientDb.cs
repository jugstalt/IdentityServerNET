using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Distribution.ValueTypes;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.HttpProxy.Services.DbContext;

public class HttpProxyClientDb : IClientDbContextModify
{
    private readonly HttpInvokerService<IClientDbContextModify> _httpInvoker;

    public HttpProxyClientDb(
            HttpInvokerService<IClientDbContextModify> httpInvoker,
            IOptions<ClientDbContextConfiguration> options)
    {
        _httpInvoker = httpInvoker;
    }

    #region IClientDbContext

    public Task<ClientModel?> FindClientByIdAsync(string clientId)
        => _httpInvoker.HandleGetAsync<ClientModel?>(
                Helper.GetMethod<IClientDbContext>(nameof(FindClientByIdAsync)),
                clientId);

    #endregion

    #region IClientDbContextModify

    public Task AddClientAsync(ClientModel client)
        => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                Helper.GetMethod<IClientDbContextModify>(nameof(AddClientAsync)),
                client);

    public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
        => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                Helper.GetMethod<IClientDbContextModify>(nameof(UpdateClientAsync)),
                client, propertyNames ?? []);


    public Task RemoveClientAsync(ClientModel client)
        => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                Helper.GetMethod<IClientDbContextModify>(nameof(RemoveClientAsync)),
                client);


    async public Task<IEnumerable<ClientModel>> GetAllClients()
        => await _httpInvoker.HandleGetAsync<IEnumerable<ClientModel>>(
                Helper.GetMethod<IClientDbContextModify>(nameof(GetAllClients))) ?? [];

    #endregion
}
