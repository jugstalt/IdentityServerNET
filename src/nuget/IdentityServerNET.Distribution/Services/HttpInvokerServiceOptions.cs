using IdentityServerNET.Distribution.Extensions;
using System.Text.Json;

namespace IdentityServerNET.Distribution.Services;

public class HttpInvokerServiceOptions<T>
{
    private readonly JsonSerializerOptions _jsonOptions;

    public HttpInvokerServiceOptions()
    {
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web).AddHttpInvokerDefaults();
    }

    public string UrlPath { get; set; } = "";

    public JsonSerializerOptions JsonOptions => _jsonOptions;
}
