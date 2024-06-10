using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    public class SecretsVaultDbContextConfiguration
    {
        public string ConnectionString { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }
    }
}
