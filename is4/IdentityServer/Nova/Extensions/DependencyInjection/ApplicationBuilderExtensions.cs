using IdentityServer.Nova.Middleware;
using Microsoft.AspNetCore.Builder;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

        return appBuilder;
    }
}
