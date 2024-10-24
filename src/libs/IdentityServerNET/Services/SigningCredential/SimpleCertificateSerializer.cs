using IdentityServerNET.Abstractions.SigningCredential;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

public class SimpleCertificateSerializer : ICertificateSerializer
{
    private readonly string _certPassword;

    public SimpleCertificateSerializer(SigningCredentialCertificateStorageOptions options)
    {
        _certPassword = options.CertPassword;
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

        return LoadFromBytes(buffer, String.Empty);
    }

    public X509Certificate2 LoadFromBytes(byte[] bytes, string name)
    {
        return new X509Certificate2(bytes, _certPassword);
    }

    async public Task WriteToFileAsync(string fileName, X509Certificate2 cert, X509ContentType type)
    {
        byte[] buffer = WriteToBytes(cert, type, String.Empty);

        using (var fs = new FileStream(fileName, FileMode.OpenOrCreate,
                        FileAccess.Write, FileShare.None, buffer.Length, true))
        {
            await fs.WriteAsync(buffer, 0, buffer.Length);
        }
    }

    public byte[] WriteToBytes(X509Certificate2 cert, X509ContentType type, string name)
    {
        return cert.Export(X509ContentType.Pfx, _certPassword);
    }

    #endregion
}
