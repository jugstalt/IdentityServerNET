using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer.Legacy.UserInteraction;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.DependencyInjection
{
    public class SecretsVaultDbContextConfiguration
    {
        public string ConnectionString { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }
    }
}
