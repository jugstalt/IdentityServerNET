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
