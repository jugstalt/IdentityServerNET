using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.SigningCredential
{
    public interface ICertificateFactory
    {
        X509Certificate2 CreateNewX509Certificate(string cn, int expireDays);
    }
}
