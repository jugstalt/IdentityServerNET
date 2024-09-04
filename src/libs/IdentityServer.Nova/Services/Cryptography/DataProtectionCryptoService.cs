using IdentityServer.Nova.Abstractions.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using System;
using System.Security.Cryptography;
using System.Text;

namespace IdentityServer.Nova.Services.Cryptography;

public class DataProtectionCryptoService : ICryptoService
{
    private readonly IDataProtector _dataProtector;

    public DataProtectionCryptoService(IDataProtectionProvider dataProtectionProvider)
    {
        _dataProtector = dataProtectionProvider.CreateProtector("CryptoServiceKey");
    }

    #region ICryptoService

    public string DecryptText(string base64Text, Encoding encoding = null)
        => _dataProtector.Unprotect(base64Text);

    public string EncryptText(string text, Encoding encoding = null)
        => _dataProtector.Protect(text);

    public string PseudoHashTextConvergent(string text, Encoding encoding = null)
    {
        if (String.IsNullOrEmpty(text))
        {
            return String.Empty;
        }

        var sha512 = SHA512.Create();
        var hashBytes = Encoding.UTF8.GetBytes(text);
        var iterations = hashBytes[0];

        for (int i = 0; i < iterations; i++)
        {
            hashBytes = sha512.ComputeHash(hashBytes);
        }

        var hashString = Convert.ToBase64String(hashBytes);

        return hashString;
    }

    #endregion
}
