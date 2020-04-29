using IdentityServer.Legacy.Services.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.SigningCredential
{
    public class SigningCredentialCertificateStorage : ISigningCredentialCertificateStorage
    {
        private readonly string _validationKeyStoragePath;
        private readonly ICertificateFactory _certificateFactory;
        private readonly ICertificateSerializer _certificateSerializer;

        public SigningCredentialCertificateStorage(
                IConfiguration configuration, 
                ICertificateFactory certificateFactory,
                ICertificateSerializer certificateSerializer = null)
        {
            _validationKeyStoragePath = configuration["SigningCredential:Storage"];
            _certificateFactory = certificateFactory;
            _certificateSerializer = certificateSerializer ?? new SimpleCertificateSerializer(configuration);
        }

        async public Task RenewCertificatesAsync(int ifOlderThanDays = 60)
        {
            await UpdateValidationKeyStorageAsync(ifOlderThanDays);
        }

        async public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync()
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
            List<X509Certificate2> certs = new List<X509Certificate2>();

            foreach(var certFile in di.GetFiles("*.pfx")
                                      .Where(f => f.CreationTime > DateTime.Now.AddDays(-60)))
            {
                X509Certificate2 cert = await _certificateSerializer.LoadFromFileAsync(certFile.FullName);
                certs.Add(cert);
            }

            return certs;
        }

        async public Task<X509Certificate2> GetRandomCertificateAsync(int maxAgeInDays)
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
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
            foreach(var s in subject.Split(',').Select(s=>s.Trim()))
            {
                if(s.ToLower().StartsWith("cn="))
                {
                    filename = subject.Substring(3).Trim();
                    break;
                }
            }

            FileInfo certFile = new FileInfo($"{ _validationKeyStoragePath }/{ filename }.pfx");
            if(!certFile.Exists)
            {
                return null;
            }

            return await _certificateSerializer.LoadFromFileAsync(certFile.FullName);
        }

        #region Helper

        async private Task UpdateValidationKeyStorageAsync(int ifOlderThanDays)
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
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

                await _certificateSerializer.WriteToFileAsync($@"{ _validationKeyStoragePath }/{ name }.pfx", cert, X509ContentType.Pfx);
            }
        }

        
        #endregion
    }
}
