using IdentityServerNET.Distribution.ValueTypes;
using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Json;
using System.Web;

namespace IdentityServerNET.Distribution.Services;

public class HttpInvokerService<TInterface>
{
    private readonly HttpClient _httpClient;
    private readonly HttpInvokerServiceOptions<TInterface> _options;

    public HttpInvokerService(HttpClient httpClient, 
        IOptions<HttpInvokerServiceOptions<TInterface>> options)
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
            var uri = $"{_options.UrlPath}/{methodInfo.Name}";

            // Prepare the query string if there are parameters
            var queryParams = string.Join("&", methodInfo.GetParameters()
                .Select((p, i) =>
                    i < parameters.Length
                    ? $"{p.Name}={HttpUtility.UrlEncode(JsonSerializer.Serialize(parameters[i], _options.JsonOptions))}"
                    : "")
                .Where(s => !string.IsNullOrEmpty(s)));

            if (!string.IsNullOrEmpty(queryParams))
            {
                uri = $"{uri}?{queryParams}";
            }

            // Send the request
            var response = await _httpClient.GetAsync(uri);
            response.EnsureSuccessStatusCode();

            if (typeof(TResult) == typeof(string))
                return NoResult.Value<TResult>();

            // Deserialize the response to the expected result type
            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TResult>(
                    resultJson,
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
            var uri = $"{_options.UrlPath}/{methodInfo.Name}";

            // Prepare the query string if there are URL parameters
            var queryParams = string.Join("&", methodInfo.GetParameters()
                .Where((p, i) => p.ParameterType != bodyParameter?.GetType())
                .Select((p, i) => 
                    i<urlParameters.Length 
                    ? $"{p.Name}={HttpUtility.UrlEncode(JsonSerializer.Serialize(urlParameters[i], _options.JsonOptions))}"
                    : ""
                    )
                .Where(s => !string.IsNullOrEmpty(s)));

            if (!string.IsNullOrEmpty(queryParams))
            {
                uri = $"{uri}?{queryParams}";
            }

            // Serialize the body parameter
            var content = new StringContent(JsonSerializer.Serialize(bodyParameter, _options.JsonOptions), System.Text.Encoding.UTF8, "application/json");

            // Send the request
            var response = await _httpClient.PostAsync(uri, content);
            response.EnsureSuccessStatusCode();

            if (typeof(TResult) == typeof(NoResult))
                return NoResult.Value<TResult>();

            // Deserialize the response to the expected result type
            var resultJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TResult>(
                    resultJson,
                    _options.JsonOptions);

            return result;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error invoking method '{methodInfo.Name}'", ex);
        }
    }   
}
