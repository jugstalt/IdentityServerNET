using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Azure.Services.DbContext
{
    static class AzureTableExtensions
    {
        static public string RowKey(this Client client)
        {
            return client.ClientId;
        }

        static public string RowKey(this ApiResource apiResource)
        {
            return apiResource.Name;
        }

        static public string RowKey(this IdentityResource identityResource)
        {
            return identityResource.Name;
        }
    }
}
