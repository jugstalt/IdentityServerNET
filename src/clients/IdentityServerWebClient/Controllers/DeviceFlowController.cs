using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace IdentityServerWebClient.Controllers;

public class DeviceFlowController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public DeviceFlowController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index() => View(new DeviceFlowInputModel
    {
        Authority = _options.Authority,
        ClientId = _options.ClientId,
        ClientSecret = _options.ClientSecret,
        Scope = _options.Scopes
    });

    [HttpPost]
    public async Task<IActionResult> Start(DeviceFlowInputModel input)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(input.Authority);
        if (disco.IsError)
        {
            ViewBag.Error = disco.Error;
            return View("Index", input);
        }

        var response = await client.RequestDeviceAuthorizationAsync(new DeviceAuthorizationRequest
        {
            Address = disco.DeviceAuthorizationEndpoint,
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope
        });

        if (response.IsError)
        {
            ViewBag.Error = $"{response.Error}: {response.ErrorDescription}";
            return View("Index", input);
        }

        HttpContext.Session.SetString($"device_authority_{response.DeviceCode}", input.Authority);
        HttpContext.Session.SetString($"device_clientid_{response.DeviceCode}", input.ClientId);
        HttpContext.Session.SetString($"device_secret_{response.DeviceCode}", input.ClientSecret);

        return View(new DeviceFlowViewModel
        {
            Authority = input.Authority,
            ClientId = input.ClientId,
            ClientSecret = input.ClientSecret,
            Scope = input.Scope,
            DeviceCode = response.DeviceCode ?? "",
            UserCode = response.UserCode ?? "",
            VerificationUri = response.VerificationUri ?? "",
            VerificationUriComplete = response.VerificationUriComplete,
            Interval = Math.Max(response.Interval, 5),
            ExpiresIn = response.ExpiresIn ?? 300
        });
    }

    [HttpGet]
    public async Task<IActionResult> Poll(string deviceCode)
    {
        var authority = HttpContext.Session.GetString($"device_authority_{deviceCode}");
        var clientId = HttpContext.Session.GetString($"device_clientid_{deviceCode}");
        var clientSecret = HttpContext.Session.GetString($"device_secret_{deviceCode}");

        if (string.IsNullOrEmpty(authority))
            return Json(new { status = "error", error = "Session expired — please restart the device flow." });

        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(authority);
        if (disco.IsError)
            return Json(new { status = "error", error = disco.Error });

        var response = await client.RequestDeviceTokenAsync(new DeviceTokenRequest
        {
            Address = disco.TokenEndpoint,
            ClientId = clientId!,
            ClientSecret = clientSecret!,
            DeviceCode = deviceCode
        });

        if (response.IsError)
        {
            if (response.Error is "authorization_pending" or "slow_down")
                return Json(new { status = "pending", error = response.Error });

            return Json(new { status = "error", error = response.Error, errorDescription = response.ErrorDescription });
        }

        var (_, accessPayload) = JwtHelper.Decode(response.AccessToken);
        var (_, idPayload) = JwtHelper.Decode(response.IdentityToken);

        return Json(new
        {
            status = "success",
            accessToken = response.AccessToken,
            idToken = response.IdentityToken,
            refreshToken = response.RefreshToken,
            rawJson = response.Raw,
            decodedAccessToken = accessPayload,
            decodedIdToken = idPayload
        });
    }
}
