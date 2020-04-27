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

namespace IdentityServer.Legacy.Services.Validation
{
    public class ValidationCertificateStorage : IValidationCertificateStorage
    {
        private readonly string _validationKeyStoragePath;
        private readonly string _certPassword;
        private readonly ICertificateFactory _certificateFactory;

        public ValidationCertificateStorage(IConfiguration configuration, ICertificateFactory certificateFactory)
        {
            _validationKeyStoragePath = configuration["ValidationKeys:Storage"];
            _certPassword = configuration["ValidationKeys:CertPassword"];
            _certificateFactory = certificateFactory;

            UpdateValidationKeyStorage();
        }

        private void UpdateValidationKeyStorage()
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
            if(!di.Exists)
            {
                di.Create();
            }

            var newestCert = di.GetFiles("*.pfx")
                               .OrderByDescending(f => f.CreationTime)
                               .FirstOrDefault();

            if (newestCert == null || (DateTime.Now - newestCert.CreationTime).TotalDays > 1)
            {
                CreateNewValidationCert(3);
            }
        }

        public IEnumerable<X509Certificate2> GetCertificates()
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
            List<X509Certificate2> certs = new List<X509Certificate2>();

            foreach(var certFile in di.GetFiles("*.pfx")
                                      .Where(f => f.CreationTime > DateTime.Now.AddDays(-60)))
            {
                X509Certificate2 cert = new X509Certificate2(certFile.FullName, _certPassword);
                certs.Add(cert);
            }

            return certs;
        }

        public X509Certificate2 GetRandomCertificate(int maxAgeInDays)
        {
            DirectoryInfo di = new DirectoryInfo(_validationKeyStoragePath);
            var certFiles = di.GetFiles("*.pfx")
                              .Where(f => f.CreationTime > DateTime.Now.AddDays(-maxAgeInDays))
                              .ToArray();

            var random = new Random();
            var certFile = certFiles[random.Next(certFiles.Length)];

            return new X509Certificate2(certFile.FullName, _certPassword);
        }

        public X509Certificate2 GetCertificate(string subject)
        {
            string filename = subject;
            foreach(var s in subject.Split(',').Select(s=>s.Trim()))
            {
                if(s.StartsWith("cn="))
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

            return new X509Certificate2(certFile.FullName, _certPassword);
        }

        #region Helper

        private void CreateNewValidationCert(int count = 1)
        {
            for (int i = 0; i < count; i++)
            {
                string name = Guid.NewGuid().ToString("N").ToLower();
                int expireDays = 36500;

                var cert = _certificateFactory.CreateNewX509Certificate(name, expireDays);
                File.WriteAllBytes($@"{ _validationKeyStoragePath }/{ name }.pfx", cert.Export(X509ContentType.Pfx, _certPassword));
            }
        }

        
        #endregion
    }
}
