using IdentityModel.Client;
using IdentityServerWebClient.Models;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityServerWebClient.Controllers;

public class UserInfoController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public UserInfoController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task<IActionResult> Index()
    {
        var authResult = await HttpContext.AuthenticateAsync();
        var savedToken = authResult?.Properties?.GetTokenValue("access_token") ?? "";

        return View(new UserInfoModel
        {
            Authority = _options.Authority,
            AccessToken = savedToken
        });
    }

    [HttpPost]
    public async Task<IActionResult> Index(UserInfoModel model)
    {
        var client = _httpClientFactory.CreateClient("identity-server");

        var disco = await client.GetDiscoveryDocumentAsync(model.Authority);
        if (disco.IsError)
        {
            model.Result = new FlowResultModel { Success = false, Error = disco.Error };
            return View(model);
        }

        var response = await client.GetUserInfoAsync(new UserInfoRequest
        {
            Address = disco.UserInfoEndpoint,
            Token = model.AccessToken
        });

        if (response.IsError)
        {
            model.Result = new FlowResultModel
            {
                Success = false,
                Error = response.Error
            };
        }
        else
        {
            var claims = response.Claims.Select(c => (c.Type, c.Value)).ToList();
            var dict = new Dictionary<string, object>();
            foreach (var c in response.Claims)
                dict[c.Type] = c.Value;
            var rawJson = JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true });

            model.Result = new FlowResultModel
            {
                Success = true,
                RawJson = rawJson,
                Claims = claims
            };
        }

        return View(model);
    }
}
