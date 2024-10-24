using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.Serialize;

namespace IdentityServerNET.Abstractions.Services;

public class SecretsVaultDbContextConfiguration
{
    public string ConnectionString { get; set; } = "";
    public ICryptoService? CryptoService { get; set; }
    public IBlobSerializer? BlobSerializer { get; set; }
}
