using System.Security.Cryptography.X509Certificates;

namespace IdentityServer.Nova.Abstractions.SigningCredential;

public interface ICertificateFactory
{
    X509Certificate2 CreateNewX509Certificate(string cn, int expireDays);
}
