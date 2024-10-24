using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public interface IAuthorizationContextService
{
    Task<AuthorizationContext> GetContextAsync();
    string GetClientId();
    string GetReturnUrlParameter(string parameter);
}
