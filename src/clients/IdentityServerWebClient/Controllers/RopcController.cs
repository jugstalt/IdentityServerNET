using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServerWebClient.Controllers;

public class RopcController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public RopcController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index() => View(new RopcModel
    {
        Authority = _options.Authority,
        ClientId = _options.ClientId,
        ClientSecret = _options.ClientSecret,
        Scope = _options.Scopes
    });

    [HttpPost]
    public async Task<IActionResult> Index(RopcModel model)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View(model);
        }

        var tokenResponse = await client.RequestPasswordTokenAsync(new PasswordTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret,
            UserName = model.Username,
            Password = model.Password,
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
            var (_, accessPayload) = JwtHelper.Decode(tokenResponse.AccessToken);
            var (_, idPayload) = JwtHelper.Decode(tokenResponse.IdentityToken);
            model.Result = new FlowResultModel
            {
                Success = true,
                AccessToken = tokenResponse.AccessToken,
                IdToken = tokenResponse.IdentityToken,
                RefreshToken = tokenResponse.RefreshToken,
                RawJson = tokenResponse.Raw,
                DecodedAccessTokenPayload = accessPayload,
                DecodedIdTokenPayload = idPayload
            };
        }

        return View(model);
    }
}
