using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;
using System.Collections.Generic;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    public class ClientDbContextConfiguration
    {
        public IEnumerable<ClientModel> IntialClients { get; set; }

        public string ConnectionString { get; set; }
        public string TableName { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }
    }
}
