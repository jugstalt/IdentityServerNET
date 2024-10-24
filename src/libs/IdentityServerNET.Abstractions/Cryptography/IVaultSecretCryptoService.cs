using System.Text;

namespace IdentityServerNET.Abstractions.Cryptography;

public interface IVaultSecretCryptoService
{
    string EncryptText(string text, Encoding? encoding = null);

    string DecryptText(string base64Text, Encoding? encoding = null);
}