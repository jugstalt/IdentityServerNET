using IdentityServer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Tests for <see cref="SecurityHeadersAttribute"/>: it used to only recognize MVC <see cref="ViewResult"/>,
/// so Razor Pages (the entire Admin area, plus login/2FA/passkey pages) never received
/// X-Content-Type-Options/X-Frame-Options/CSP/Referrer-Policy at all, since Razor Pages render a
/// <see cref="PageResult"/> instead.
/// </summary>
public class SecurityHeadersAttributeTests
{
    private static ResultExecutingContext CreateContext(IActionResult result)
    {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ResultExecutingContext(actionContext, new List<IFilterMetadata>(), result, controller: new object());
    }

    [Fact]
    public void PageResult_ReceivesSecurityHeaders()
    {
        var context = CreateContext(new PageResult());

        new SecurityHeadersAttribute().OnResultExecuting(context);

        var headers = context.HttpContext.Response.Headers;
        Assert.Equal("nosniff", headers["X-Content-Type-Options"]);
        Assert.Equal("SAMEORIGIN", headers["X-Frame-Options"]);
        Assert.True(headers.ContainsKey("Content-Security-Policy"));
        Assert.True(headers.ContainsKey("X-Content-Security-Policy"));
        Assert.Equal("no-referrer", headers["Referrer-Policy"]);
    }

    [Fact]
    public void ViewResult_StillReceivesSecurityHeaders()
    {
        var context = CreateContext(new ViewResult());

        new SecurityHeadersAttribute().OnResultExecuting(context);

        Assert.True(context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"));
    }

    [Fact]
    public void OtherResultTypes_AreLeftUntouched()
    {
        var context = CreateContext(new EmptyResult());

        new SecurityHeadersAttribute().OnResultExecuting(context);

        Assert.False(context.HttpContext.Response.Headers.ContainsKey("X-Frame-Options"));
    }

    [Fact]
    public void DoesNotOverwrite_AnAlreadySetHeader()
    {
        // Mirrors AccountController's captcha-image action, which sets a deliberately looser CSP
        // (allowing data: URIs) before the result-executing filter runs - the global filter must not
        // clobber that with the stricter default.
        var context = CreateContext(new PageResult());
        context.HttpContext.Response.Headers["Content-Security-Policy"] = "default-src 'self' data:;";

        new SecurityHeadersAttribute().OnResultExecuting(context);

        Assert.Equal("default-src 'self' data:;", context.HttpContext.Response.Headers["Content-Security-Policy"]);
    }
}
