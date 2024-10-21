using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Web;

namespace IdentityServer.Nova.Distribution.Services;

public class HttpInvokerService<TInterface>
{
    private readonly HttpClient _httpClient;
    private readonly HttpInvokerServiceOptions _options;

    public HttpInvokerService(HttpClient httpClient, IOptions<HttpInvokerServiceOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<TResult?> HandleGetAsync<TResult>(
                MethodInfo methodInfo,
                params object[] parameters
        )
    {
        try
        {
            // Construct the request URI using the method name
            var uri = $"{_options.BaseUrl}/{_options.UrlPath}/{methodInfo.Name}";

            // Prepare the query string if there are parameters
            var queryParams = string.Join("&", methodInfo.GetParameters()
                .Select((p, i) => $"{p.Name}={HttpUtility.UrlEncode(parameters[i]?.ToString())}"));

            if (!string.IsNullOrEmpty(queryParams))
            {
                uri = $"{uri}?{queryParams}";
            }

            // Send the request
            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            // Deserialize the response to the expected result type
            var result = JsonSerializer.Deserialize<TResult>(
                    await response.Content.ReadAsStringAsync(),
                    _options.JsonOptions);

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error invoking method '{methodInfo.Name}'", ex);
        }
    }

    public async Task<TResult?> HandlePostAsync<TResult, TPostType>(
                MethodInfo methodInfo,
                TPostType bodyParameter,
                params object[] urlParameters)
    {
        try
        {
            // Construct the request URI using the method name
            var uri = $"{_options.BaseUrl}/{_options.UrlPath}/{methodInfo.Name}";

            // Prepare the query string if there are URL parameters
            var queryParams = string.Join("&", methodInfo.GetParameters()
                .Where((p, i) => p.ParameterType != bodyParameter?.GetType())
                .Select((p, i) => $"{p.Name}={HttpUtility.UrlEncode(urlParameters[i]?.ToString())}"));

            if (!string.IsNullOrEmpty(queryParams))
            {
                uri = $"{uri}?{queryParams}";
            }

            // Serialize the body parameter
            var content = new StringContent(JsonSerializer.Serialize(bodyParameter), System.Text.Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();

            // Deserialize the response to the expected result type
            var result = JsonSerializer.Deserialize<TResult>(
                    await response.Content.ReadAsStringAsync(),
                    _options.JsonOptions);

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error invoking method '{methodInfo.Name}'", ex);
        }
    }
}
