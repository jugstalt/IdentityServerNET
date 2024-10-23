using System.Text.Json;

namespace IdentityServer.Nova.Distribution.Services;

public class HttpInvokerServiceOptions<T>
{
    public string UrlPath { get; set; } = "";

    public JsonSerializerOptions JsonOptions { get; } =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);
}
