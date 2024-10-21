using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Text.Json;

namespace IdentityServer.Nova.Distribution.Services;

public class InvokerService<T>
{
    private readonly T _service;

    public InvokerService(T service)
    {
        _service = service;
    }

    public async Task<object?> HandleAsync(HttpRequest request, string methodName)
    {
        try
        {
            MethodInfo? method = typeof(T).GetMethod(methodName);
            if (method is null)
            {
                return new { Error = $"Method '{methodName}' not found." };
            }

            var parameters = method.GetParameters() ?? [];
            var arguments = new object?[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];

                if (String.IsNullOrEmpty(param.Name)) continue;

                if (request.Query.ContainsKey(param.Name))
                {
                    arguments[i] = Convert.ChangeType(request.Query[param.Name].ToString(), param.ParameterType);
                }
                else if (request.Body != null && (param.ParameterType.IsClass && param.ParameterType != typeof(string)))
                {
                    arguments[i] = await JsonSerializer.DeserializeAsync(request.Body, param.ParameterType);
                }
                else
                {
                    return new { Error = $"Missing or invalid parameter: {param.Name}" };
                }
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
