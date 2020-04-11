using IdentityModel;
using IdentityServer.Legacy.Services.Cryptography;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy
{
    static public class Extensions
    {
        static public string NameToHexId(this string name, ICryptoService cryptoService=null)
        {
            if(cryptoService == null)
            {
                cryptoService = new Base64CryptoService();
            }
            var encryptedUserName = cryptoService.EncryptTextUnsalted(name.Trim().ToUpper());
            return ByteArrayToString(Encoding.UTF8.GetBytes(encryptedUserName.ToSha256()));
        }

        static private string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);

            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString().ToLower();
        }
    }
}
