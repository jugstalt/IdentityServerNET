using IdentityModel;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Services.Cryptography;
using System;
using System.Text;

namespace IdentityServerNET;

static public class CryptoExtensions
{
    static public string NameToHexId(this string name, ICryptoService cryptoService = null)
    {
        if (String.IsNullOrWhiteSpace(name))
        {
            return String.Empty;
        }

        if (cryptoService == null)
        {
            cryptoService = new Base64CryptoService();
        }
        var encryptedUserName = cryptoService.PseudoHashTextConvergent(name.Trim().ToUpper());
        return ByteArrayToString(Encoding.UTF8.GetBytes(encryptedUserName.ToSha256()));
    }

    static private string ByteArrayToString(byte[] ba)
    {
        StringBuilder hex = new StringBuilder(ba.Length * 2);

        foreach (byte b in ba)
        {
            hex.AppendFormat("{0:x2}", b);
        }

        return hex.ToString().ToLower();
    }
}
