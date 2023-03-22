using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services
{
    public interface IAuthorizationContextService
    {
        Task<AuthorizationContext> GetContextAsync();
        string GetClientId();
        string GetReturnUrlParameter(string parameter);
    }
}
