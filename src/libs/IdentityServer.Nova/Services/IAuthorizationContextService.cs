using System.Threading.Tasks;

namespace IdentityServer.Nova.Services;

public interface IAuthorizationContextService
{
    Task<AuthorizationContext> GetContextAsync();
    string GetClientId();
    string GetReturnUrlParameter(string parameter);
}
