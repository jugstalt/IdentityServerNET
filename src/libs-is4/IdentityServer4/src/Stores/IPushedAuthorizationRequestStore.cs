using System.Collections.Specialized;
using System.Threading.Tasks;

namespace IdentityServer4.Stores;

/// <summary>
/// Store for pushed authorization requests (PAR, RFC 9126).
/// </summary>
public interface IPushedAuthorizationRequestStore
{
    Task StoreAsync(string requestUri, NameValueCollection parameters, int expiresInSeconds = 60);
    Task<NameValueCollection> GetAsync(string requestUri);
    Task RemoveAsync(string requestUri);
}
