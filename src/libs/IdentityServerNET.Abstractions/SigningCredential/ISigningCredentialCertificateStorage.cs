using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.SigningCredential;

public interface ISigningCredentialCertificateStorage
{
    Task RenewCertificatesAsync(int ifOlderThanDays = 60);

    Task<IEnumerable<X509Certificate2>> GetCertificatesAsync();

    Task<X509Certificate2> GetRandomCertificateAsync(int maxAgeInDays);

    Task<X509Certificate2> GetCertificateAsync(string subject);
}
