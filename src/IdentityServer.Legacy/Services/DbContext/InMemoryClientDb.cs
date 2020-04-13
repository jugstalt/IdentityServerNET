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
    public class InMemoryClientDb : IClientDbContext, IClientDbContextModify
    {
        private static ConcurrentDictionary<string, Client> _clients = null;

        public InMemoryClientDb(IOptions<ClientDbContextConfiguration> options = null)
        {
            if(_clients==null)
            {
                _clients = new ConcurrentDictionary<string, Client>();

                // Init from configuration
                if(options?.Value?.IntialClients != null)
                {
                    foreach(var client in options.Value.IntialClients)
                    {
                        _clients[client.ClientId] = client;
                    }
                }
            }
        }

        #region IClientDbContext

        public Task<Client> FindClientByIdAsync(string clientId)
        {
            if (_clients.ContainsKey(clientId))
            {
                return Task.FromResult(_clients[clientId]);
            }

            return Task.FromResult<Client>(null);
        }

        #endregion

        #region IClientDbContextModify

        public Task AddClientAsync(Client client)
        {
            if(_clients.ContainsKey(client.ClientId))
            {
                throw new Exception($"Client with clientId { client.ClientId } already exists");
            }

            _clients[client.ClientId] = client;

            return Task.FromResult(0);
        }

        public Task UpdateClientAsync(Client client, IEnumerable<string> propertyNames = null)
        {
            if (!_clients.ContainsKey(client.ClientId))
            {
                throw new Exception($"Client with clientId { client.ClientId } not exists");
            }

            _clients[client.ClientId] = client;

            return Task.FromResult(0);
        }

        public Task RemoveClientAsync(Client client)
        {
            if (!_clients.ContainsKey(client.ClientId))
            {
                throw new Exception($"Client with clientId { client.ClientId } not exists");
            }

            if(!_clients.TryRemove(client.ClientId, out client))
            {
                throw new Exception($"Can't remove client");
            }

            return Task.FromResult(0);
        }

        public Task<IEnumerable<Client>> GetAllClients()
        {
            return Task.FromResult<IEnumerable<Client>>(_clients.Values.ToArray());
        }

        #endregion
    }
}
