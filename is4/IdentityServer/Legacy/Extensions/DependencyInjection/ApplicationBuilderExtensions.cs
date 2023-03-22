using IdentityServer.Legacy.Middleware;
using Microsoft.AspNetCore.Builder;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    static public class ApplicationBuilderExtensions
    {
        static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
        {
            appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

            return appBuilder;
        }
    }
}
