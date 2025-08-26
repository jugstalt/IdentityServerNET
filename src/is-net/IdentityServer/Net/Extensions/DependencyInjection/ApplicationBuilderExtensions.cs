using IdentityServerNET.Middleware;
using Microsoft.AspNetCore.Builder;
using System;

namespace IdentityServerNET.Extensions.DependencyInjection;

static public class ApplicationBuilderExtensions
{
    static public IApplicationBuilder AddXForwardedProtoMiddleware(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseMiddleware<XForwardedProtoMiddleware>();

        return appBuilder;
    }

    static public IApplicationBuilder UseIdentityServerAppBasePath(this IApplicationBuilder app)
    {
        var basePath = Environment.GetEnvironmentVariable("IDENTITYSERVER_APP_BASE_PATH");
        if (!String.IsNullOrEmpty(basePath))
        {
            app.UsePathBase(basePath);
            Console.WriteLine($"Info: Set Base Path: {basePath}");
        }

        return app;
    }
}
