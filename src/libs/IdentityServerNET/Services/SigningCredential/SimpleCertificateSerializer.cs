using IdentityServerNET.Abstractions.SigningCredential;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

public class SimpleCertificateSerializer : ICertificateSerializer
{
    // Certificates written before per-installation random passwords were introduced (see
    // ServiceCollectionExtensions.AddSigningCredentialCertificateStorage) were protected with this
    // fixed, publicly-known default. Kept only so pre-existing files stay readable after upgrading -
    // never used to protect newly written files.
    private const string LegacyDefaultCertPassword = "Secu4epas3wOrd";

    private const X509KeyStorageFlags KeyStorageFlags =
        X509KeyStorageFlags.MachineKeySet
        | X509KeyStorageFlags.PersistKeySet
        | X509KeyStorageFlags.Exportable;

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
            await fs.ReadExactlyAsync(buffer, 0, buffer.Length);
        }

        return LoadFromBytes(buffer, String.Empty);
    }

    public X509Certificate2 LoadFromBytes(byte[] bytes, string name)
    {
        try
        {
            return X509CertificateLoader.LoadPkcs12(bytes, _certPassword, KeyStorageFlags);
        }
        catch (CryptographicException) when (_certPassword != LegacyDefaultCertPassword)
        {
            return X509CertificateLoader.LoadPkcs12(bytes, LegacyDefaultCertPassword, KeyStorageFlags);
        }
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
