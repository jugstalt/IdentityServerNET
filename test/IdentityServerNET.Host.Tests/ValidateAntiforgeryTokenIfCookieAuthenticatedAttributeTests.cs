using IdentityServer.Api.Controllers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using System.Security.Claims;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Security tests for <see cref="ValidateAntiforgeryTokenIfCookieAuthenticatedAttribute"/>, which
/// closes a CSRF gap on <c>SigningController.Post</c>: that endpoint accepts both the
/// <c>Identity.Application</c> cookie and a Bearer token, form-encodes its payload, and mints a
/// signed JWT with attacker-influenceable claims - a cross-site auto-submitting form could have
/// abused a logged-in admin's cookie session. The fix must block unproven cookie-authenticated
/// requests without breaking bearer-token clients, which never carry the antiforgery cookie at all.
/// </summary>
public class ValidateAntiforgeryTokenIfCookieAuthenticatedAttributeTests
{
    private static IServiceProvider CreateAntiforgeryServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        services.AddAntiforgery();
        return services.BuildServiceProvider();
    }

    private static ActionExecutingContext CreateActionContext(HttpContext httpContext)
    {
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            controller: new object());
    }

    [Fact]
    public async Task BearerAuthenticated_SkipsAntiforgeryValidation()
    {
        var httpContext = new DefaultHttpContext { RequestServices = CreateAntiforgeryServices() };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Bearer-Signing"));
        // No antiforgery cookie/token anywhere - a real Bearer client would never have one.

        var context = CreateActionContext(httpContext);
        var nextCalled = false;

        await new ValidateAntiforgeryTokenIfCookieAuthenticatedAttribute().OnActionExecutionAsync(
            context, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        Assert.True(nextCalled, "Bearer-authenticated requests must not require an antiforgery token.");
        Assert.Null(context.Result);
    }

    [Fact]
    public async Task CookieAuthenticated_WithoutAntiforgeryToken_IsRejected()
    {
        var httpContext = new DefaultHttpContext { RequestServices = CreateAntiforgeryServices() };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: IdentityConstants.ApplicationScheme));
        // Simulates a cross-site form POST: cookie-authenticated (the browser attaches it
        // automatically), but with no antiforgery token - exactly what an attacker's page can produce.

        var context = CreateActionContext(httpContext);
        var nextCalled = false;

        await new ValidateAntiforgeryTokenIfCookieAuthenticatedAttribute().OnActionExecutionAsync(
            context, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        Assert.False(nextCalled, "The action must not run without a valid antiforgery token.");
        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CookieAuthenticated_WithValidAntiforgeryToken_Proceeds()
    {
        var services = CreateAntiforgeryServices();
        var antiforgery = services.GetRequiredService<IAntiforgery>();

        // "Request 1": mint a token pair, as a legitimate same-origin page with
        // @Html.AntiForgeryToken() would when first rendered.
        var tokenHttpContext = new DefaultHttpContext { RequestServices = services };
        var tokens = antiforgery.GetAndStoreTokens(tokenHttpContext);
        var setCookie = tokenHttpContext.Response.Headers["Set-Cookie"][0]!;
        var cookiePair = setCookie.Split(';')[0];

        // "Request 2": a separate HttpContext carrying the cookie back plus the form-posted token -
        // using a fresh context (rather than reusing the one above) avoids relying on antiforgery's
        // own per-request Items cache and more accurately simulates two distinct HTTP requests.
        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Method = "POST";
        httpContext.Request.ContentType = "application/x-www-form-urlencoded";
        httpContext.Request.Headers["Cookie"] = cookiePair;
        httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["__RequestVerificationToken"] = tokens.RequestToken
        });
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: IdentityConstants.ApplicationScheme));

        var context = CreateActionContext(httpContext);
        var nextCalled = false;

        await new ValidateAntiforgeryTokenIfCookieAuthenticatedAttribute().OnActionExecutionAsync(
            context, () => { nextCalled = true; return Task.FromResult<ActionExecutedContext>(null!); });

        Assert.True(nextCalled, "A request with a valid antiforgery token must be allowed through.");
        Assert.Null(context.Result);
    }
}
