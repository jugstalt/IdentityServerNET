#nullable enable

using IdentityServer4.Services;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Security.Claims;
using System.Threading.Tasks;

namespace IdentityServer.Net.Middleware;

/// <summary>
/// Pre-populates HttpContext.Items["UIRealm"] and HttpContext.Items["UIRealmTitle"] so that
/// every login-flow view and the layout can use realm branding without their own DB lookups.
///
/// Detection priority (first match wins):
///   1. Authenticated user  → email domain → realm
///   2. TempData["LoginPendingRealm"] → set during identifier step, survives 2FA redirects
///   3. returnUrl query param → IS4 authorization context → client realm
/// </summary>
public class UIRealmMiddleware
{
    private readonly RequestDelegate _next;

    public UIRealmMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(
        HttpContext context,
        IRealmDbContext? realmDb = null,
        IIdentityServerInteractionService? is4 = null)
    {
        if (realmDb is not null)
        {
            string? realmName = null;

            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Authenticated: derive realm from email claim.
                var email = context.User.FindFirstValue(ClaimTypes.Email)
                         ?? context.User.FindFirstValue("email");
                var at = email?.LastIndexOf('@') ?? -1;
                if (at > 0)
                {
                    var domain = email![(at + 1)..].ToLowerInvariant();
                    var realm = await realmDb.FindByDomainAsync(domain, context.RequestAborted);
                    realmName = realm?.Name;
                }
            }
            else
            {
                // Login flow: check TempData first (Scenario 2 — realm user, no realm client).
                var factory = context.RequestServices
                    .GetService(typeof(ITempDataDictionaryFactory)) as ITempDataDictionaryFactory;
                if (factory is not null)
                {
                    var tempData = factory.GetTempData(context);
                    realmName = tempData.Peek("LoginPendingRealm") as string;
                }

                // Fallback: parse IS4 auth context from returnUrl (Scenario 1 — realm client).
                if (realmName is null && is4 is not null)
                {
                    var returnUrl = context.Request.Query["returnUrl"].ToString()
                                 ?? context.Request.Query["ReturnUrl"].ToString();
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        var authCtx = await is4.GetAuthorizationContextAsync(returnUrl);
                        realmName = authCtx?.Client?.ClientId?.GetRealm();
                    }
                }
            }

            if (realmName is not null)
            {
                context.Items["UIRealm"] = realmName;

                // Pre-load the realm title so every view can read it from Context.Items
                // without needing its own async DB call.
                var realmModel = await realmDb.FindByNameAsync(realmName, context.RequestAborted);
                var title = realmModel?.Appearance?.ApplicationTitle;
                if (!string.IsNullOrEmpty(title))
                    context.Items["UIRealmTitle"] = title;
            }
        }

        await _next(context);
    }
}
