using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Distribution.ValueTypes;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using System.Reflection;

namespace IdentityServer.Nova.HttpProxy.Services.DbContext
{
    internal class HttpProxyClientDb<T> : IClientDbContextModify
        where T : IClientDbContext
    {
        private readonly HttpInvokerService<T> _httpInvoker;

        public HttpProxyClientDb(HttpInvokerService<T> httpInvoker)
        {
            _httpInvoker = httpInvoker;
        }

        #region IClientDbContext

        public Task<ClientModel?> FindClientByIdAsync(string clientId)
            => _httpInvoker.HandleGetAsync<ClientModel?>(
                    GetMethod<T>(nameof(FindClientByIdAsync)),
                    clientId);

        #endregion

        #region IClientDbContextModify

        public Task AddClientAsync(ClientModel client)
            => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                    GetMethod<T>(nameof(AddClientAsync)),
                    client);

        public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
            => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                    GetMethod<T>(nameof(RemoveClientAsync)),
                    client, propertyNames ?? []);


        public Task RemoveClientAsync(ClientModel client)
            => _httpInvoker.HandlePostAsync<NoResult, ClientModel>(
                    GetMethod<T>(nameof(RemoveClientAsync)),
                    client);


        async public Task<IEnumerable<ClientModel>> GetAllClients()
            => await _httpInvoker.HandleGetAsync<IEnumerable<ClientModel>>(
                    GetMethod<T>(nameof(FindClientByIdAsync))) ?? [];

        #endregion

        #region Helper

        private static MethodInfo GetMethod<T>(string methodName)
            => typeof(T).GetMethod(methodName)
               ?? throw new InvalidOperationException($"Method {methodName} not found.");

        #endregion
    }
}
