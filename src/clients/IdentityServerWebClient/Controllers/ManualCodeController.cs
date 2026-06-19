using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace IdentityServerWebClient.Controllers;

public class ManualCodeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public ManualCodeController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/ManualCode/Callback";
        return View(new ManualCodeInputModel
        {
            Authority = _options.Authority,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Scope = _options.Scopes,
            RedirectUri = redirectUri
        });
    }

    [HttpPost]
    public async Task<IActionResult> Build(ManualCodeInputModel input)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(input.Authority);
        if (disco.IsError)
        {
            ViewBag.Error = disco.Error;
            return View("Index", input);
        }

        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);
        var state = GenerateState();

        HttpContext.Session.SetString($"pkce_verifier_{state}", codeVerifier);
        HttpContext.Session.SetString($"pkce_authority_{state}", input.Authority);
        HttpContext.Session.SetString($"pkce_clientid_{state}", input.ClientId);
        HttpContext.Session.SetString($"pkce_secret_{state}", input.ClientSecret);
        HttpContext.Session.SetString($"pkce_redirect_{state}", input.RedirectUri);

        var authorizeUrl = QueryHelpers.AddQueryString(disco.AuthorizeEndpoint!, new Dictionary<string, string?>
        {
            ["client_id"] = input.ClientId,
            ["response_type"] = "code",
            ["scope"] = input.Scope,
            ["redirect_uri"] = input.RedirectUri,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        });

        return View(new ManualCodeBuildModel
        {
            Authority = input.Authority,
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope,
            RedirectUri = input.RedirectUri,
            AuthorizeUrl = authorizeUrl,
            State = state,
            CodeVerifier = codeVerifier,
            CodeChallenge = codeChallenge
        });
    }

    public IActionResult Callback(string? code, string? state, string? error, string? error_description)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return View(new ManualCodeCallbackModel
            {
                Result = new FlowResultModel { Success = false, Error = error, ErrorDescription = error_description }
            });
        }

        var codeVerifier = HttpContext.Session.GetString($"pkce_verifier_{state}") ?? "";
        var authority = HttpContext.Session.GetString($"pkce_authority_{state}") ?? _options.Authority;
        var clientId = HttpContext.Session.GetString($"pkce_clientid_{state}") ?? _options.ClientId;
        var clientSecret = HttpContext.Session.GetString($"pkce_secret_{state}") ?? _options.ClientSecret;
        var redirectUri = HttpContext.Session.GetString($"pkce_redirect_{state}")
                          ?? $"{Request.Scheme}://{Request.Host}/ManualCode/Callback";

        return View(new ManualCodeCallbackModel
        {
            Code = code ?? "",
            State = state ?? "",
            Authority = authority,
            ClientId = clientId,
            ClientSecret = clientSecret,
            RedirectUri = redirectUri,
            CodeVerifier = codeVerifier
        });
    }

    [HttpPost]
    public async Task<IActionResult> Exchange(ManualCodeCallbackModel model)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View("Callback", model);
        }

        var response = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret,
            Code = model.Code,
            RedirectUri = model.RedirectUri,
            CodeVerifier = model.CodeVerifier
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

        return View("Callback", model);
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string codeVerifier)
    {
        var bytes = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(bytes);
    }

    private static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
