#nullable enable

using IdentityServerNET.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Threading.Tasks;

namespace IdentityServer.Net.Middleware;

/// <summary>
/// Blocks every request from an authenticated user whose <see cref="ApplicationUser.MustChangePassword"/>
/// flag is set, redirecting to <see cref="ChangePasswordRequiredPath"/> instead. This is the single
/// enforcement point for one-time/temporary passwords — it runs after every sign-in path (password,
/// passkey, external login) rather than duplicating the check in each of them.
/// </summary>
public class MustChangePasswordMiddleware
{
    private const string ChangePasswordRequiredPath = "/Identity/Account/ChangePasswordRequired";

    // Requests that must still work while a password change is pending: the gate page itself, the way
    // out (logout), and the branding assets the gate page needs to render.
    private static readonly string[] AllowedPathPrefixes =
    {
        ChangePasswordRequiredPath,
        "/Account/Logout",
        "/Identity/Account/Logout",
        "/ui",
        "/connect",
        "/.well-known",
    };

    private readonly RequestDelegate _next;

    public MustChangePasswordMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true && !IsAllowedPath(context.Request.Path))
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user?.MustChangePassword == true)
            {
                var returnUrl = $"{context.Request.Path}{context.Request.QueryString}";
                context.Response.Redirect($"{ChangePasswordRequiredPath}?ReturnUrl={Uri.EscapeDataString(returnUrl)}");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsAllowedPath(PathString path)
    {
        var value = path.Value ?? "/";
        foreach (var prefix in AllowedPathPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
