using IdentityServer.Legacy.Middleware;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
