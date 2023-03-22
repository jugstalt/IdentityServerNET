using IdentityServer.Legacy.Models.IdentityServerWrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IResourceDbContextModify : IResourceDbContext
    {
        Task AddApiResourceAsync(ApiResourceModel apiResource);
        Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string> propertyNames = null);
        Task RemoveApiResourceAsync(ApiResourceModel apiResource);

        Task AddIdentityResourceAsync(IdentityResourceModel identityResource);
        Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string> propertyNames = null);
        Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource);
    }
}
