using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.Cryptography
{
    public interface ICryptoService
    {
        string EncryptText(string text, Encoding encoding = null);
        
        string EncryptTextConvergent(string text, Encoding encoding = null);

        string DecryptText(string base64Text, Encoding encoding = null);
        
        string DecryptTextConvergent(string base64Text, Encoding encoding = null);
    }
}