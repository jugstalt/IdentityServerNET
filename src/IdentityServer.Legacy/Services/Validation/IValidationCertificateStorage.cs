using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IdentityServer.Legacy.Services.Validation
{
    public interface IValidationCertificateStorage
    {
        IEnumerable<X509Certificate2> GetCertificates();

        X509Certificate2 GetRandomCertificate(int maxAgeInDays);

        X509Certificate2 GetCertificate(string subject);
    }
}
