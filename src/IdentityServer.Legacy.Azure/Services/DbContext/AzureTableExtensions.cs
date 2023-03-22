using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer4.Models;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    static class AzureTableExtensions
    {
        static public string RowKey(this ClientModel client)
        {
            return client.ClientId;
        }

        static public string RowKey(this ApiResourceModel apiResource)
        {
            return apiResource.Name;
        }

        static public string RowKey(this IdentityResourceModel identityResource)
        {
            return identityResource.Name;
        }
    }
}
