using Microsoft.AspNetCore.Mvc;

namespace IdentityServerWebClient.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
