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
    class ClientStore : IClientStore
    {
        public ClientStore(IClientDbContext clientDbContext)
        {
            _clientDbContext = clientDbContext;
        }

        private IClientDbContext _clientDbContext = null;

        async public Task<Client> FindClientByIdAsync(string clientId)
        {
            return (await _clientDbContext.FindClientByIdAsync(clientId))?.IdentityServer4Instance;
        }
    }
}
