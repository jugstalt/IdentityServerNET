using IdentityModel.Client;
using IdentityServerWebClient.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace IdentityServerWebClient.Controllers;

public class DiscoveryController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TestClientOptions _options;

    public DiscoveryController(IHttpClientFactory httpClientFactory, IOptions<TestClientOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public IActionResult Index()
    {
        ViewBag.Authority = _options.Authority;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string authority)
    {
        ViewBag.Authority = authority;

        var client = _httpClientFactory.CreateClient("identity-server");
        var disco = await client.GetDiscoveryDocumentAsync(authority);

        if (disco.IsError)
        {
            ViewBag.Error = disco.Error;
        }
        else
        {
            var doc = JsonDocument.Parse(disco.Raw!);
            ViewBag.DiscoveryJson = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }

        return View();
    }
}
