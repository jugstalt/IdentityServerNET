using IdentityServerNET.Middleware;
using Microsoft.AspNetCore.Builder;

namespace IdentityServerNET.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

        return appBuilder;
    }
}
