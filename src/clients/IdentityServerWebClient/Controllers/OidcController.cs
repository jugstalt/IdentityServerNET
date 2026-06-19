using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServerWebClient.Controllers;

public class OidcController : Controller
{
    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var authResult = await HttpContext.AuthenticateAsync();
        ViewBag.AuthProperties = authResult?.Properties?.Items ?? new Dictionary<string, string?>();
        return View();
    }

    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Profile");

        return Challenge(
            new AuthenticationProperties { RedirectUri = "/Oidc/Profile" },
            "oidc");
    }

    public IActionResult Logout() =>
        SignOut(
            new AuthenticationProperties { RedirectUri = "/Oidc/LoggedOut" },
            "Cookies", "oidc");

    public IActionResult LoggedOut() => View();
}
