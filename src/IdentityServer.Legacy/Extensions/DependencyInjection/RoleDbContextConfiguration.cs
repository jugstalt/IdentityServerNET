using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.Serialize;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    public class RoleDbContextConfiguration
    {
        public string ConnectionString { get; set; }
        public ICryptoService CryptoService { get; set; }
        public IBlobSerializer BlobSerializer { get; set; }
    }
}
