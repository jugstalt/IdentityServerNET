using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.Cryptography
{
    public interface ICryptoService
    {
        string EncryptText(string text, Encoding encoding = null);
        
        string EncryptTextUnsalted(string text, Encoding encoding = null);

        string DecryptText(string base64Text, Encoding encoding = null);
        
        string DecryptTextUnsalted(string base64Text, Encoding encoding = null);
    }
}