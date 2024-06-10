using IdentityServer.Nova.Models.IdentityServerWrappers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public interface IResourceDbContext
    {
        Task<ApiResourceModel> FindApiResourceAsync(string name);
        Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames);
        Task<IEnumerable<ApiResourceModel>> GetAllApiResources();

        Task<IdentityResourceModel> FindIdentityResource(string name);
        Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources();
    }
}
