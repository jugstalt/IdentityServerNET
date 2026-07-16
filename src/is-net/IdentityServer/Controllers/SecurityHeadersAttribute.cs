// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServer;

// Registered as a global MVC filter in Program.cs (options.Filters.Add<SecurityHeadersAttribute>()),
// so it applies to every MVC view AND every Razor Page automatically - Razor Pages render a
// PageResult rather than a ViewResult, which is why this previously needed to be added by hand to
// each controller and never covered the Admin area or the Identity account pages at all.
public class SecurityHeadersAttribute : ActionFilterAttribute
{
    public override void OnResultExecuting(ResultExecutingContext context)
    {
        var result = context.Result;
        if (result is ViewResult || result is PageResult)
        {
            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Content-Type-Options
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Type-Options"))
            {
                context.HttpContext.Response.Headers.Append("X-Content-Type-Options", "nosniff");
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/X-Frame-Options
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"))
            {
                context.HttpContext.Response.Headers.Append("X-Frame-Options", "SAMEORIGIN");
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Security-Policy
            // script-src/style-src need 'unsafe-inline': this app's Razor views rely throughout on
            // inline <script> blocks (e.g. the 2FA QR code setup, the admin user search/filter) and
            // inline style="..." attributes (e.g. the ASP.NET Core validation-summary placeholder
            // <li style="display:none">). Without an explicit 'unsafe-inline' here they silently
            // inherit the stricter default-src 'self' instead - browsers then ignore every inline
            // style/script rather than erroring, so the breakage isn't obvious: it looks like a rendering
            // bug (a hidden validation-summary <li> reappearing as a bullet) rather than a CSP block.
            // img-src data: is required for Bootstrap SVG checkmarks and inline SVG icons used in CSS background-image
            var csp = "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';";
            // also consider adding upgrade-insecure-requests once you have HTTPS in place for production
            //csp += "upgrade-insecure-requests;";
            // also an example if you need client images to be displayed from twitter
            // csp += "img-src 'self' https://pbs.twimg.com;";

            // once for standards compliant browsers
            if (!context.HttpContext.Response.Headers.ContainsKey("Content-Security-Policy"))
            {
                context.HttpContext.Response.Headers.Append("Content-Security-Policy", csp);
            }
            // and once again for IE
            if (!context.HttpContext.Response.Headers.ContainsKey("X-Content-Security-Policy"))
            {
                context.HttpContext.Response.Headers.Append("X-Content-Security-Policy", csp);
            }

            // https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Referrer-Policy
            var referrer_policy = "no-referrer";
            if (!context.HttpContext.Response.Headers.ContainsKey("Referrer-Policy"))
            {
                context.HttpContext.Response.Headers.Append("Referrer-Policy", referrer_policy);
            }
        }
    }
}
