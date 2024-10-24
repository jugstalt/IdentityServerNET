using System.Security.Cryptography.X509Certificates;

namespace IdentityServerNET.Abstractions.SigningCredential;

public interface ICertificateFactory
{
    X509Certificate2 CreateNewX509Certificate(string cn, int expireDays);
}
