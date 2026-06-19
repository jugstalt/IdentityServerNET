using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServerWebClient.Controllers;

public class ClientCredentialsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public ClientCredentialsController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index() => View(new ClientCredentialsModel
    {
        Authority = _options.Authority,
        ClientId = string.IsNullOrEmpty(_options.ApiClientId) ? _options.ClientId : _options.ApiClientId,
        ClientSecret = string.IsNullOrEmpty(_options.ApiClientSecret) ? _options.ClientSecret : _options.ApiClientSecret,
        Scope = string.IsNullOrEmpty(_options.ApiScopes) ? _options.Scopes : _options.ApiScopes
    });

    [HttpPost]
    public async Task<IActionResult> Index(ClientCredentialsModel model)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View(model);
        }

        var tokenResponse = await client.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret,
            Scope = model.Scope
        });

        if (tokenResponse.IsError)
        {
            model.Result = new FlowResultModel
            {
                Success = false,
                Error = tokenResponse.Error,
                ErrorDescription = tokenResponse.ErrorDescription,
                RawJson = tokenResponse.Raw
            };
        }
        else
        {
            var (_, payload) = JwtHelper.Decode(tokenResponse.AccessToken);
            model.Result = new FlowResultModel
            {
                Success = true,
                AccessToken = tokenResponse.AccessToken,
                RawJson = tokenResponse.Raw,
                DecodedAccessTokenPayload = payload
            };
        }

        return View(model);
    }
}
