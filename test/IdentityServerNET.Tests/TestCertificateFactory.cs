using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IdentityServerNET.Abstractions.SigningCredential;

namespace IdentityServerNET.Tests;

/// <summary>
/// Test implementation of <see cref="ICertificateFactory"/> that produces a self-signed
/// RSA certificate, mirroring the production CertificateFactory. Used to exercise the
/// signing-credential storage without referencing the web application.
/// </summary>
internal sealed class TestCertificateFactory : ICertificateFactory
{
    public X509Certificate2 CreateNewX509Certificate(string cn, int expireDays)
        => Create(cn, expireDays);

    public static X509Certificate2 Create(string cn, int expireDays = 365)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"cn={cn}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        return request.CreateSelfSigned(
            DateTimeOffset.Now.AddMinutes(-5),
            DateTimeOffset.Now.AddDays(expireDays));
    }
}
