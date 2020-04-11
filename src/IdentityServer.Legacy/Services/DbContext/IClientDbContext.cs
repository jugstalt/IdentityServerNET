using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IClientDbContext
    {
        Task<Client> FindClientByIdAsync(string clientId);
    }
}
