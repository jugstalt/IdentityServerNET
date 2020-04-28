using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.SigningCredential;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Factories
{
    public class CertificateFactory : ICertificateFactory
    {

        public X509Certificate2 CreateNewX509Certificate(string cn, int expireDays)
        {
            var ecdsa = RSA.Create(); // generate asymmetric key pair
            var req = new CertificateRequest($"cn={ cn }", ecdsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddDays(expireDays));

            return cert;
        }
    }
}
