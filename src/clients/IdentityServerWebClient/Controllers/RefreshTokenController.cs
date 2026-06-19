using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServerWebClient.Controllers;

public class RefreshTokenController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public RefreshTokenController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<IActionResult> Index()
    {
        var authResult = await HttpContext.AuthenticateAsync();
        var savedRefreshToken = authResult?.Properties?.GetTokenValue("refresh_token") ?? "";

        return View(new RefreshTokenModel
        {
            Authority = _options.Authority,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            RefreshToken = savedRefreshToken
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(RefreshTokenModel model)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View(model);
        }

        var response = await client.RequestRefreshTokenAsync(new RefreshTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret,
            RefreshToken = model.RefreshToken
        });

        if (response.IsError)
        {
            model.Result = new FlowResultModel
            {
                Success = false,
                Error = response.Error,
                ErrorDescription = response.ErrorDescription,
                RawJson = response.Raw
            };
        }
        else
        {
            var (_, accessPayload) = JwtHelper.Decode(response.AccessToken);
            var (_, idPayload) = JwtHelper.Decode(response.IdentityToken);
            model.Result = new FlowResultModel
            {
                Success = true,
                AccessToken = response.AccessToken,
                IdToken = response.IdentityToken,
                RefreshToken = response.RefreshToken,
                RawJson = response.Raw,
                DecodedAccessTokenPayload = accessPayload,
                DecodedIdTokenPayload = idPayload
            };
        }

        return View(model);
    }
}
