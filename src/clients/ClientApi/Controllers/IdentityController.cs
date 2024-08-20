using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace ClientApi.Controllers;

[Route("identity")]
[Authorize(AuthenticationSchemes = "Bearer", Policy = "command")]
[ApiController]
public class IdentityController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return new JsonResult(from c in User.Claims select new { c.Type, c.Value });
    }
}