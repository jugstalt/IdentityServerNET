using IdentityServer.Legacy.Services.Signing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer.Legacy.Extensions;
using IdentityServer.Legacy.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace IdentityServer.Legacy.Controllers
{
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer-Signing, Identity.Application")]
    [ApiController]
    public class SigningController : ControllerBase
    {
        private readonly CustomTokenService _customToken;

        public SigningController(CustomTokenService customToken)
        {
            _customToken = customToken;
        }

        [HttpPost]
        async public Task<IActionResult> Post([FromForm] string data)
        {
            try
            {
                if(String.IsNullOrEmpty(data))
                {
                    throw new StatusMessageException("data is NULL");
                }
                var token = _customToken.CreateCustomToken(data);
                var tokenString = await _customToken.CreateSecurityTokenAsync(token);

                //var jwtToken = await tokenString.ToValidatedJwtSecurityToken(token.Issuer);

                return new JsonResult(new
                {
                    success = true,
                    token = tokenString
                });
            }
            catch (StatusMessageException sme)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        errorMessage = sme.Message
                    });
            }
            catch /*(Exception ex)*/
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        errorMessage = "Internal error."
                    });
            }
        }
    }
}
