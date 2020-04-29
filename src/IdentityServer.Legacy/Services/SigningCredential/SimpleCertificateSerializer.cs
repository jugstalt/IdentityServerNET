using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.SigningCredential
{
    public class SimpleCertificateSerializer : ICertificateSerializer
    {
        private readonly string _certPassword;

        public SimpleCertificateSerializer(IConfiguration configuration)
        {
            _certPassword = configuration["SigningCredential:CertPassword"];
        }

        #region ICertificateSerializer

        async public Task<X509Certificate2> LoadFromFileAsync(string fileName)
        {
            byte[] buffer;
            using (var fs = File.Open(fileName, FileMode.Open))
            {
                buffer = new byte[fs.Length];
                await fs.ReadAsync(buffer, 0, buffer.Length);
            }

            return new X509Certificate2(buffer, _certPassword);
        }

        async public Task WriteToFileAsync(string fileName, X509Certificate2 cert, X509ContentType type)
        {
            byte[] buffer = cert.Export(X509ContentType.Pfx, _certPassword);

            using (var fs = new FileStream(fileName, FileMode.OpenOrCreate,
                            FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }
        }

        #endregion
    }
}
