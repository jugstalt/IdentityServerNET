using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientWeb.Controllers;

//[Authorize(Roles = "role_read")]
[Authorize]
public class PortalController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Logout()
    {
        return SignOut("Cookies", "oidc");
    }
}