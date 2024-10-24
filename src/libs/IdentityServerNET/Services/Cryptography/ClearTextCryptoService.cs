using IdentityServerNET.Abstractions.Cryptography;
using System.Text;

namespace IdentityServerNET.Services.Cryptography;

public class ClearTextCryptoService : ICryptoService
{
    #region ICryptoService

    public string EncryptText(string text, Encoding encoding = null)
    {
        return text;
    }

    public string PseudoHashTextConvergent(string text, Encoding encoding = null)
    {
        return text;
    }

    public string DecryptText(string base64Text, Encoding encoding = null)
    {
        return base64Text;
    }

    //public string DecryptTextConvergent(string base64Text, Encoding encoding = null)
    //{
    //    return base64Text;
    //}

    #endregion
}
