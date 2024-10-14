using Google.Protobuf.WellKnownTypes;
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
        return new JsonResult(
            new {
                claims = (User.Claims ?? [])
                    .Select(c => new { name = c.Type, value = c.Value })
                    .ToArray()
            });
    }
}