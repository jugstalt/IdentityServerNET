using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace IdentityServerWebClient.Controllers;

public class PushedAuthorizationController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public PushedAuthorizationController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index()
    {
        var redirectUri = $"{Request.Scheme}://{Request.Host}/PushedAuthorization/Callback";
        return View(new PushedAuthorizationInputModel
        {
            Authority = _options.Authority,
            ClientId = _options.ClientId,
            ClientSecret = _options.ClientSecret,
            Scope = _options.Scopes,
            RedirectUri = redirectUri
        });
    }

    [HttpPost]
    public async Task<IActionResult> Push(PushedAuthorizationInputModel input)
    {
        var httpClient = _httpClientFactory.CreateClient("identity-server");

        var disco = await httpClient.GetDiscoveryDocumentAsync(input.Authority);
        if (disco.IsError)
        {
            ViewBag.Error = disco.Error;
            return View("Index", input);
        }

        // determine PAR endpoint from discovery document, fallback to convention
        string parEndpoint = $"{input.Authority.TrimEnd('/')}/connect/par";
        if (!string.IsNullOrEmpty(disco.Raw))
        {
            try
            {
                using var doc = JsonDocument.Parse(disco.Raw);
                if (doc.RootElement.TryGetProperty("pushed_authorization_request_endpoint", out var parProp))
                    parEndpoint = parProp.GetString() ?? parEndpoint;
            }
            catch { }
        }

        var state = GenerateState();
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Step 1: POST parameters to PAR endpoint (Backchannel, incl. PKCE)
        var parParams = new Dictionary<string, string>
        {
            ["client_id"] = input.ClientId,
            ["client_secret"] = input.ClientSecret,
            ["response_type"] = "code",
            ["scope"] = input.Scope,
            ["redirect_uri"] = input.RedirectUri,
            ["state"] = state,
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var parRequestContent = new FormUrlEncodedContent(parParams);
        var parRequestRaw = string.Join("&", parParams
            .Where(p => p.Key != "client_secret")
            .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"))
            + "&client_secret=***";

        var parResponse = await httpClient.PostAsync(parEndpoint, parRequestContent);
        var parResponseBody = await parResponse.Content.ReadAsStringAsync();

        if (!parResponse.IsSuccessStatusCode)
        {
            ViewBag.Error = $"PAR request failed ({(int)parResponse.StatusCode}): {parResponseBody}";
            return View("Index", input);
        }

        using var parJson = JsonDocument.Parse(parResponseBody);
        var requestUri = parJson.RootElement.GetProperty("request_uri").GetString() ?? "";
        var expiresIn = parJson.RootElement.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 60;

        // save state for callback
        HttpContext.Session.SetString($"par_authority_{state}", input.Authority);
        HttpContext.Session.SetString($"par_clientid_{state}", input.ClientId);
        HttpContext.Session.SetString($"par_secret_{state}", input.ClientSecret);
        HttpContext.Session.SetString($"par_redirect_{state}", input.RedirectUri);
        HttpContext.Session.SetString($"par_verifier_{state}", codeVerifier);

        // Step 2: Build the authorize URL (only request_uri + client_id needed)
        var authorizeUrl = QueryHelpers.AddQueryString(disco.AuthorizeEndpoint!, new Dictionary<string, string?>
        {
            ["client_id"] = input.ClientId,
            ["request_uri"] = requestUri
        });

        return View(new PushedAuthorizationPushedModel
        {
            Authority = input.Authority,
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope,
            RedirectUri = input.RedirectUri,
            RequestUri = requestUri,
            ExpiresIn = expiresIn,
            AuthorizeUrl = authorizeUrl,
            State = state,
            CodeVerifier = codeVerifier,
            CodeChallenge = codeChallenge,
            ParRequestRaw = parRequestRaw,
            ParResponseRaw = FormatJson(parResponseBody)
        });
    }

    public IActionResult Callback(string? code, string? state, string? error, string? error_description)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return View(new PushedAuthorizationCallbackModel
            {
                Result = new FlowResultModel { Success = false, Error = error, ErrorDescription = error_description }
            });
        }

        var authority = HttpContext.Session.GetString($"par_authority_{state}") ?? _options.Authority;
        var clientId = HttpContext.Session.GetString($"par_clientid_{state}") ?? _options.ClientId;
        var clientSecret = HttpContext.Session.GetString($"par_secret_{state}") ?? _options.ClientSecret;
        var redirectUri = HttpContext.Session.GetString($"par_redirect_{state}")
                          ?? $"{Request.Scheme}://{Request.Host}/PushedAuthorization/Callback";
        var codeVerifier = HttpContext.Session.GetString($"par_verifier_{state}") ?? "";

        return View(new PushedAuthorizationCallbackModel
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
    public async Task<IActionResult> Exchange(PushedAuthorizationCallbackModel model)
    {
        var httpClient = _httpClientFactory.CreateClient("identity-server");

        var disco = await httpClient.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View("Callback", model);
        }

        var response = await httpClient.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
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

    private static string GenerateState()
    {
        var bytes = RandomNumberGenerator.GetBytes(16);
        return Base64UrlEncode(bytes);
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

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string FormatJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return json;
        }
    }
}
