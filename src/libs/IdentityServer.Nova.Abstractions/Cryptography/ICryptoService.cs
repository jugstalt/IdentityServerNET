using System.Text;

namespace IdentityServer.Nova.Abstractions.Cryptography;

public interface ICryptoService : IVaultSecretCryptoService
{
    string PseudoHashTextConvergent(string text, Encoding? encoding = null);
}