using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.Serialize;

namespace IdentityServer.Nova.Abstractions.Services;

public class SecretsVaultDbContextConfiguration
{
    public string ConnectionString { get; set; }
    public ICryptoService CryptoService { get; set; }
    public IBlobSerializer BlobSerializer { get; set; }
}
