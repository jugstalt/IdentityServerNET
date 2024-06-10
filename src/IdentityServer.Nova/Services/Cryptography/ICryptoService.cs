using System.Text;

namespace IdentityServer.Nova.Services.Cryptography
{
    public interface IVaultSecretCryptoService
    {
        string EncryptText(string text, Encoding encoding = null);

        string DecryptText(string base64Text, Encoding encoding = null);
    }

    public interface ICryptoService : IVaultSecretCryptoService
    {
        string EncryptTextConvergent(string text, Encoding encoding = null);

        string DecryptTextConvergent(string base64Text, Encoding encoding = null);
    }
}