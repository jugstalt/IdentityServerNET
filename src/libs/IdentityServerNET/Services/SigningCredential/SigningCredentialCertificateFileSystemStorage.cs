using IdentityServerNET.Abstractions.SigningCredential;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.SigningCredential;

public class SigningCredentialCertificateStorageOptions
{
    public string Storage { get; set; }
    public string CertPassword { get; set; }
}

public class SigningCredentialCertificateFileSystemStorage : ISigningCredentialCertificateStorage
{
    private readonly SigningCredentialCertificateStorageOptions _options;
    private readonly ICertificateFactory _certificateFactory;
    private readonly ICertificateSerializer _certificateSerializer;

    public SigningCredentialCertificateFileSystemStorage(
            IOptions<SigningCredentialCertificateStorageOptions> options,
            ICertificateFactory certificateFactory,
            ICertificateSerializer certificateSerializer = null)
    {
        _options = options.Value;
        _certificateFactory = certificateFactory;
        _certificateSerializer = certificateSerializer ?? new SimpleCertificateSerializer(_options);

        var di = new DirectoryInfo(_options.Storage);
        if (!di.Exists)
        {
            di.Create();
        }
    }

    async public Task RenewCertificatesAsync(int ifOlderThanDays = 60)
    {
        await UpdateValidationKeyStorageAsync(ifOlderThanDays);
    }

    async public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync()
    {
        DirectoryInfo di = new DirectoryInfo(_options.Storage);
        List<X509Certificate2> certs = new List<X509Certificate2>();

        foreach (var certFile in di.GetFiles("*.pfx")
                                  .Where(f => f.CreationTime > DateTime.Now.AddDays(-60)))
        {
            X509Certificate2 cert = await _certificateSerializer.LoadFromFileAsync(certFile.FullName);
            certs.Add(cert);
        }

        return certs;
    }

    async public Task<X509Certificate2> GetRandomCertificateAsync(int maxAgeInDays)
    {
        DirectoryInfo di = new DirectoryInfo(_options.Storage);
        var certFiles = di.GetFiles("*.pfx")
                          .Where(f => f.CreationTime > DateTime.Now.AddDays(-maxAgeInDays))
                          .ToArray();

        var random = new Random();
        var certFile = certFiles[random.Next(certFiles.Length)];

        return await _certificateSerializer.LoadFromFileAsync(certFile.FullName);
    }

    async public Task<X509Certificate2> GetCertificateAsync(string subject)
    {
        string filename = subject;
        foreach (var s in subject.Split(',').Select(s => s.Trim()))
        {
            if (s.ToLower().StartsWith("cn="))
            {
                filename = subject.Substring(3).Trim();
                break;
            }
        }

        FileInfo certFile = new FileInfo($"{_options.Storage}/{filename}.pfx");
        if (!certFile.Exists)
        {
            return null;
        }

        return await _certificateSerializer.LoadFromFileAsync(certFile.FullName);
    }

    #region Helper

    async private Task UpdateValidationKeyStorageAsync(int ifOlderThanDays)
    {
        DirectoryInfo di = new DirectoryInfo(_options.Storage);
        if (!di.Exists)
        {
            di.Create();
        }

        var newestCert = di.GetFiles("*.pfx")
                           .OrderByDescending(f => f.CreationTime)
                           .FirstOrDefault();

        if (newestCert == null || (DateTime.Now - newestCert.CreationTime).TotalDays > ifOlderThanDays)
        {
            await CreateNewValidationCertAsync();
        }
    }

    async private Task CreateNewValidationCertAsync(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            string name = Guid.NewGuid().ToString("N").ToLower();
            int expireDays = 36500;

            var cert = _certificateFactory.CreateNewX509Certificate(name, expireDays);

            await _certificateSerializer.WriteToFileAsync($@"{_options.Storage}/{name}.pfx", cert, X509ContentType.Pfx);
        }
    }


    #endregion
}
