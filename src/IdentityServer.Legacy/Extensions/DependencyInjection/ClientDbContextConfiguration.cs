using IdentityServer.Legacy.Models.IdentityServerWrappers;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
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
