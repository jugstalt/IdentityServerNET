using IdentityServerNET.Distribution.Extensions;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json;

namespace IdentityServerNET.Distribution.Services;

public class InvokerService<T>
{
    private readonly T _service;
    private readonly JsonSerializerOptions _jsonOptions;

    public InvokerService(T service)
    {
        _service = service;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web).AddHttpInvokerDefaults();
    }

    public async Task<object?> HandleAsync(HttpRequest request, string methodName)
    {
        try
        {
            MethodInfo? method = _service?.GetType().GetMethod(methodName);
            if (method is null)
            {
                return new { Error = $"Method '{methodName}' not found." };
            }

            var parameters = method.GetParameters() ?? [];
            var arguments = new object?[parameters.Length];
            List<Type> genericMethodParameterTypes = new();

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (String.IsNullOrEmpty(param.Name)) continue;

                if (request.Query.ContainsKey(param.Name))
                {
                    if (param.ParameterType.IsGenericParameter)
                    {
                        var jsonObject = JsonSerializer.Deserialize(
                            request.Query[param.Name].ToString(), typeof(object),
                            _jsonOptions
                            );
                        arguments[i] = jsonObject switch
                        {
                            JsonElement jsonElement => jsonElement.ValueKind switch
                            {
                                JsonValueKind.String => jsonElement.GetString(),
                                JsonValueKind.False => false,
                                JsonValueKind.True => true,
                                JsonValueKind.Null => null,
                                JsonValueKind.Number when jsonElement.GetString()?.Contains(".") == true => jsonElement.GetDouble(),
                                JsonValueKind.Number => jsonElement.GetInt64(),
                                _ => throw new Exception($"Unsupportet generic paramter kind {jsonElement.ValueKind}")
                            },
                            _ => throw new Exception("Can't determine generic parameter")
                        };
                        genericMethodParameterTypes.Add(arguments[i]?.GetType() ?? typeof(object));
                    }
                    else
                    {
                        var jsonObject = JsonSerializer.Deserialize(
                            request.Query[param.Name].ToString(), param.ParameterType,
                            _jsonOptions);
                        arguments[i] = jsonObject switch
                        {
                            // if param.ParameterType is object the result is often just 
                            // JsonElement => convert this to its real type
                            JsonElement jsonElement => jsonElement.ValueKind switch
                            {
                                JsonValueKind.String => jsonElement.GetString(),
                                JsonValueKind.False => false,
                                JsonValueKind.True => true,
                                JsonValueKind.Null => null,
                                JsonValueKind.Number when jsonElement.GetString()?.Contains(".") == true => jsonElement.GetDouble(),
                                JsonValueKind.Number => jsonElement.GetInt64(),
                                _ => throw new Exception($"Unsupportet paramter kind {jsonElement.ValueKind}")
                            },
                            _ => jsonObject
                        };
                    }

                }
                else if (request.Body != null && (param.ParameterType.IsClass && param.ParameterType != typeof(string)))
                {
                    arguments[i] = await JsonSerializer.DeserializeAsync(request.Body, param.ParameterType, _jsonOptions);
                }
                else if (param.ParameterType == typeof(CancellationToken))
                {
                    arguments[i] = CancellationToken.None;
                }
                else
                {
                    if (param.ParameterType == typeof(CancellationToken))
                        return new { Error = $"Missing or invalid parameter: {param.Name}" };
                }
            }

            if (method.IsGenericMethod)
            {
                method = method.MakeGenericMethod(genericMethodParameterTypes.ToArray());
            }

            var result = method.Invoke(_service, arguments);

            if (result is Task taskResult)
            {
                await taskResult;

                if (taskResult.GetType().IsGenericType)
                {
                    var resultProperty = taskResult.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(taskResult);
                }
                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            return new { Error = $"{ex.Message}: {ex.InnerException?.Message}".Trim() };
        }
    }
}
