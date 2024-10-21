using System.Text.Json;

namespace IdentityServer.Nova.Distribution.Services;

public class HttpInvokerServiceOptions
{
    public string BaseUrl { get; set; } = "";
    public string UrlPath { get; set; } = "";

    public JsonSerializerOptions JsonOptions { get; } = new JsonSerializerOptions();
}
