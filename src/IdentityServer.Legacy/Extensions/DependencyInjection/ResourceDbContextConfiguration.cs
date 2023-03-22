using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.Serialize;
using System.Collections.Generic;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    public class ResourceDbContextConfiguration
    {
        public IEnumerable<ApiResourceModel> InitialApiResources { get; set; }
        public IEnumerable<IdentityResourceModel> InitialIdentityResources { get; set; }

        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }
    }
}
