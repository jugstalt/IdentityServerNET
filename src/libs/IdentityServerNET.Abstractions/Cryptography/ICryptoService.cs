using System.Text;

namespace IdentityServerNET.Abstractions.Cryptography;

public interface ICryptoService : IVaultSecretCryptoService
{
    string PseudoHashTextConvergent(string text, Encoding? encoding = null);
}