using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IResourceDbContextModify : IResourceDbContext
    {
        Task AddApiResourceAsync(ApiResource apiResource);
        Task UpdateApiResourceAsync(ApiResource apiResource);
        Task RemoveApiResourceAsync(ApiResource apiResource);

        Task<IEnumerable<ApiResource>> GetAllApiResources();
    }
}
