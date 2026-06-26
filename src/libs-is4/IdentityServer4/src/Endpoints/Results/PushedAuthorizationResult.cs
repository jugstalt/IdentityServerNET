using IdentityServer4.Extensions;
using IdentityServer4.Hosting;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace IdentityServer4.Endpoints.Results;

internal class PushedAuthorizationResult : IEndpointResult
{
    public string RequestUri { get; }
    public int ExpiresIn { get; }

    public PushedAuthorizationResult(string requestUri, int expiresIn)
    {
        RequestUri = requestUri;
        ExpiresIn = expiresIn;
    }

    public async Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = 201;
        context.Response.SetNoCache();

        await context.Response.WriteJsonAsync(new
        {
            request_uri = RequestUri,
            expires_in = ExpiresIn
        });
    }
}
