using System.Text;

namespace IdentityServer.Nova.Abstractions.Cryptography;

public interface ICryptoService : IVaultSecretCryptoService
{
    string EncryptTextConvergent(string text, Encoding? encoding = null);

    string DecryptTextConvergent(string base64Text, Encoding? encoding = null);
}