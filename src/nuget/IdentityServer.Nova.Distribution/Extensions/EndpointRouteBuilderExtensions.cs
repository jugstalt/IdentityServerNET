using IdentityServer.Nova.Distribution.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;

namespace IdentityServer.Nova.Distribution.Extensions;

static public class EndpointRouteBuilderExtensions
{
    static public IEndpointRouteBuilder MapInvokeEndpoints<T>(
            this IEndpointRouteBuilder builder,
            string path = "/api/invoke")
    {
        builder.MapGet($"{path}/{{methodName}}", async (
            HttpRequest request,
            [FromServices] InvokerService<T> invoker,
            string methodName) =>
            {
                var invokeResult = await invoker.HandleAsync(request, methodName);

                return invokeResult;
            }
        );

        builder.MapPost($"{path}/{{methodName}}", async (
                    HttpRequest request,
                    [FromServices] InvokerService<T> invoker,
                    string methodName) =>
            {
                var invokeResult = await invoker.HandleAsync(request, methodName);

                return invokeResult switch
                {
                    string => JsonSerializer.Serialize(invokeResult),
                    _ => invokeResult
                };
            }
        );

        return builder;
    }
}
