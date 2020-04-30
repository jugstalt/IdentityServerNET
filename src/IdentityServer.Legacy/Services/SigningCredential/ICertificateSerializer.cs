using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.SigningCredential
{
    public interface ICertificateSerializer
    {
        X509Certificate2 LoadFromBytes(byte[] bytes, string name);
        Task<X509Certificate2> LoadFromFileAsync(string fileName);

        Task WriteToFileAsync(string fileName, X509Certificate2 cert, X509ContentType type);
        byte[] WriteToBytes(X509Certificate2 cert, X509ContentType type, string name);
    }
}
