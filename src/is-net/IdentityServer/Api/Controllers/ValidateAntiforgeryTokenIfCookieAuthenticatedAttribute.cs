using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Api.Controllers;

/// <summary>
/// Requires a valid antiforgery token, but only for requests authenticated via the
/// <see cref="IdentityConstants.ApplicationScheme"/> cookie. Requests authenticated via a bearer
/// token (e.g. external API clients using client credentials) are exempt - they never carry the
/// antiforgery cookie in the first place, so requiring it unconditionally would break them, while
/// leaving cookie-authenticated POSTs unprotected would allow a cross-site form submission to act
/// on behalf of a logged-in browser session (CSRF).
/// </summary>
public class ValidateAntiforgeryTokenIfCookieAuthenticatedAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var isCookieAuthenticated = context.HttpContext.User.Identities
            .Any(identity => identity.IsAuthenticated && identity.AuthenticationType == IdentityConstants.ApplicationScheme);

        if (isCookieAuthenticated)
        {
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<IAntiforgery>();

            try
            {
                await antiforgery.ValidateRequestAsync(context.HttpContext);
            }
            catch (AntiforgeryValidationException)
            {
                context.Result = new BadRequestObjectResult(new
                {
                    success = false,
                    errorMessage = "Missing or invalid antiforgery token."
                });
                return;
            }
        }

        await next();
    }
}
