using IdentityModel.Client;
using IdentityServerWebClient.Helpers;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityServerWebClient.Controllers;

public class TokenInspectorController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public TokenInspectorController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<IActionResult> Index()
    {
        var authResult = await HttpContext.AuthenticateAsync();
        var savedToken = authResult?.Properties?.GetTokenValue("access_token") ?? "";

        return View(new TokenInspectorModel
        {
            Token = savedToken,
            Authority = _options.Authority,
            IntrospectionClientId = string.IsNullOrEmpty(_options.ApiClientId) ? _options.ClientId : _options.ApiClientId,
            IntrospectionClientSecret = string.IsNullOrEmpty(_options.ApiClientSecret) ? _options.ClientSecret : _options.ApiClientSecret
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(TokenInspectorModel model, string action)
    {
        var (header, payload) = JwtHelper.Decode(model.Token);
        model.Header = header;
        model.Payload = payload;

        if (action == "introspect" && !string.IsNullOrWhiteSpace(model.Authority))
        {
            var client = _httpClientFactory.CreateClient("identity-server");

            var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
            if (disco.IsError)
            {
                model.Error = disco.Error;
            }
            else
            {
                var response = await client.IntrospectTokenAsync(new TokenIntrospectionRequest
                {
                    Address = disco.IntrospectionEndpoint,
                    ClientId = model.IntrospectionClientId,
                    ClientSecret = model.IntrospectionClientSecret,
                    Token = model.Token
                });

                if (response.IsError)
                {
                    model.Error = response.Error;
                }
                else
                {
                    model.IsActive = response.IsActive;
                    var dict = new Dictionary<string, object>();
                    foreach (var c in response.Claims)
                        dict[c.Type] = c.Value;
                    model.IntrospectionResult = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });
                }
            }
        }

        return View(model);
    }
}
