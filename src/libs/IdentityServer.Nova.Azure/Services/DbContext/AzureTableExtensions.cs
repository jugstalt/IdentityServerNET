using IdentityServer.Nova.Models.IdentityServerWrappers;

namespace IdentityServer.Nova.Azure.Services.DbContext;

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
