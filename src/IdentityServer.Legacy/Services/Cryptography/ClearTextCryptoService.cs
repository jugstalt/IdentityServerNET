using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.Cryptography
{
    public class ClearTextCryptoService : ICryptoService
    {
        #region ICryptoService

        public string EncryptText(string text, Encoding encoding = null)
        {
            return text;
        }

        public string EncryptTextUnsalted(string text, Encoding encoding = null)
        {
            return text;
        }

        public string DecryptText(string base64Text, Encoding encoding = null)
        {
            return base64Text;
        }

        public string DecryptTextUnsalted(string base64Text, Encoding encoding=null)
        {
            return base64Text;
        }

        #endregion
    }
}
