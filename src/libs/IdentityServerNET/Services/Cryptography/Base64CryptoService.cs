﻿using IdentityServerNET.Abstractions.Cryptography;
using System;
using System.Text;

namespace IdentityServerNET.Services.Cryptography;

public class Base64CryptoService : ICryptoService
{
    #region ICryptoService

    public string EncryptText(string text, Encoding encoding = null)
    {
        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        var bytes = encoding == null ?
            Encoding.UTF8.GetBytes(text) :
            encoding.GetBytes(text);

        return Convert.ToBase64String(bytes);
    }

    public string PseudoHashTextConvergent(string text, Encoding encoding = null)
    {
        return EncryptText(text, encoding);
    }

    public string DecryptText(string base64Text, Encoding encoding = null)
    {
        var decryptedBytes = Convert.FromBase64String(base64Text);

        if (encoding == null)
        {
            return Encoding.UTF8.GetString(decryptedBytes);
        }

        return encoding.GetString(decryptedBytes);
    }

    //public string DecryptTextConvergent(string base64Text, Encoding encoding = null)
    //{
    //    return DecryptText(base64Text, encoding);
    //}

    #endregion
}
