﻿using System.Text;

namespace IdentityServer.Nova.Services.Cryptography
{
    public class ClearTextCryptoService : ICryptoService
    {
        #region ICryptoService

        public string EncryptText(string text, Encoding encoding = null)
        {
            return text;
        }

        public string EncryptTextConvergent(string text, Encoding encoding = null)
        {
            return text;
        }

        public string DecryptText(string base64Text, Encoding encoding = null)
        {
            return base64Text;
        }

        public string DecryptTextConvergent(string base64Text, Encoding encoding = null)
        {
            return base64Text;
        }

        #endregion
    }
}
