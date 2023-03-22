using IdentityServer.Legacy.Exceptions;
using IdentityServer.Legacy.Services.Signing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Specialized;
using System.Threading.Tasks;

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
        async public Task<IActionResult> Post(int lifeTime = 3600)
        {
            try
            {
                if (!Request.HasFormContentType)
                {
                    throw new StatusMessageException("No form data to sign");
                }

                NameValueCollection claims = new NameValueCollection();

                foreach (string formKey in Request.Form.Keys)
                {
                    if (!formKey.Equals("lifetime", StringComparison.InvariantCultureIgnoreCase))
                    {
                        claims[formKey] = Request.Form[formKey];
                    }
                }

                var token = _customToken.CreateCustomToken(claims, lifeTime);
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
