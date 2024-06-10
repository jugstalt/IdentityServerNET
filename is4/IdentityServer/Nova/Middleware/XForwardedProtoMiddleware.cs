using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Middleware
{
    public class XForwardedProtoMiddleware
    {
        private readonly RequestDelegate _next;

        public XForwardedProtoMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            var xProtoHeader = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (xProtoHeader != null && xProtoHeader.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Scheme = "https";
            }

            await _next.Invoke(context);
        }
    }
}
