using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IResourceDbContext
    {
        Task<ApiResource> FindApiResourceAsync(string name);
        Task<IEnumerable<ApiResource>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames);
        Task<IEnumerable<ApiResource>> GetAllApiResources();

        Task<IdentityResource> FindIdentityResource(string name);
        Task<IEnumerable<IdentityResource>> GetAllIdentityResources();
    }
}
