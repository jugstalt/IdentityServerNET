using IdentityServer.Nova.Services.SigningCredential;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace IdentityServer.Nova.Factories
{
    public class CertificateFactory : ICertificateFactory
    {

        public X509Certificate2 CreateNewX509Certificate(string cn, int expireDays)
        {
            var ecdsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={cn}", ecdsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(expireDays));

            return cert;
        }
    }
}
