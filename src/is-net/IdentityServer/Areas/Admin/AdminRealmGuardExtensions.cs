using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin;

/// <summary>
/// Read-side realm hardening for the admin edit pages. A realm admin can only load realm-scoped
/// entities of its own realm; the system admin only global ones. This complements the write-side
/// ownership checks in the store decorators, so a foreign realm's entity cannot even be viewed by
/// crafting a direct URL.
/// </summary>
internal static class AdminRealmGuardExtensions
{
    /// <summary>
    /// Whether the caller (resolved from its current realm) may access the entity with the given
    /// realm-scoped identifier. Uses <see cref="IRealmContext"/> from the request services.
    /// </summary>
    public static async Task<bool> IsInCurrentRealmAsync(this PageModel page, string identifier)
    {
        var realm = await page.HttpContext.RequestServices
            .GetRequiredService<IRealmContext>()
            .GetCurrentRealmNameAsync();

        return identifier.BelongsToRealm(realm);
    }
}
