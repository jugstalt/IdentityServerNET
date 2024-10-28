#nullable enable

using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer;

[SecurityHeaders]
[AllowAnonymous]
public class ExternalController : Controller
{
    private readonly IUserStore<ApplicationUser> _users;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly ILogger<ExternalController> _logger;
    private readonly IEventService _events;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public ExternalController(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IEventService events,
        ILogger<ExternalController> logger,
        IUserStore<ApplicationUser> users,
        SignInManager<ApplicationUser> signInManager)
    {
        _users = users;

        _interaction = interaction;
        _clientStore = clientStore;
        _logger = logger;
        _events = events;
        _signInManager = signInManager;
    }

    /// <summary>
    /// initiate roundtrip to external authentication provider
    /// </summary>
    [HttpGet]
    public IActionResult Challenge(string scheme, string returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
        {
            returnUrl = "~/";
        }

        // validate returnUrl - either it is a valid OIDC URL or back to a local page
        if (Url.IsLocalUrl(returnUrl) == false && _interaction.IsValidReturnUrl(returnUrl) == false)
        {
            // user might have clicked on a malicious link - should be logged
            throw new Exception("invalid return URL");
        }

        // start challenge and roundtrip the return URL and scheme 
        var props = new AuthenticationProperties
        {
            RedirectUri = $"{Url.Action(nameof(Callback))}?__as={scheme}",
            Items =
            {
                { "returnUrl", returnUrl },
                { "scheme", scheme },
            }
        };

        return Challenge(props, scheme);
    }

    /// <summary>
    /// Post processing of external authentication
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Callback()
    {
        // read external identity from the temporary cookie
        string externalAuthScheme = Request.Query["__as"]!;
        var result = await HttpContext.AuthenticateAsync(externalAuthScheme);
        if (result?.Succeeded != true)
        {
            throw new Exception("External authentication error");
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var externalClaims = result.Principal?.Claims.Select(c => $"{c.Type}: {c.Value}");
            _logger.LogDebug("External claims: {@claims}", externalClaims);
        }

        // lookup our user and external provider info
        var (user, provider, providerUserId, claims) = await FindUserFromExternalProvider(result);
        if (user == null)
        {
            // this might be where you might initiate a custom workflow for user registration
            // in this sample we don't show how that would be done, as our sample implementation
            // simply auto-provisions new external user
            user = AutoProvisionUser(provider, providerUserId, claims);
        }

        if (user is null)
        {
            throw new Exception("Can't determine external user");
        }

        // this allows us to collect any additional claims or properties
        // for the specific protocols used and store them in the local auth cookie.
        // this is typically used to store data needed for signout from those protocols.
        var additionalLocalClaims = new List<Claim>();
        var localSignInProps = new AuthenticationProperties();

        ProcessLoginCallback(result, additionalLocalClaims, localSignInProps);

        // issue authentication cookie for user
        var isuser = new IdentityServerUser(user.Id)
        {
            DisplayName = user.UserName,
            IdentityProvider = provider,
            AdditionalClaims = additionalLocalClaims
        };

        
        //await HttpContext.SignInAsync("Identity.Application", isuser.CreatePrincipal()/*, localSignInProps*/);
        await _signInManager.SignInAsync(user, localSignInProps);

        // delete temporary cookie used during external authentication
        await HttpContext.SignOutAsync($"{externalAuthScheme}");
        await HttpContext.SignOutAsync($"{externalAuthScheme}.cookie");

        // retrieve return URL
        var returnUrl = result.Properties?.Items["returnUrl"] ?? "~/";

        // check if external login is in the context of an OIDC request
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        await _events.RaiseAsync(new UserLoginSuccessEvent(provider, providerUserId, user.Id, user.UserName, true, context?.Client.ClientId));

        if (context != null)
        {
            if (context.IsNativeClient())
            {
                // The client is native, so this change in how to
                // return the response is for better UX for the end user.
                return this.LoadingPage("Redirect", returnUrl);
            }
        }

        return Redirect(returnUrl);
    }

    async private Task<(ApplicationUser? user, string provider, string providerUserId, IEnumerable<Claim> claims)> FindUserFromExternalProvider(AuthenticateResult result)
    {
        var externalUser = result.Principal;

        // try to determine the unique id of the external user (issued by the provider)
        // the most common claim type for that are the sub claim and the NameIdentifier
        // depending on the external provider, some other claim type might be used
        var userIdClaim = externalUser?.FindFirst(JwtClaimTypes.Subject) ??
                          externalUser?.FindFirst(ClaimTypes.NameIdentifier) ??
                          throw new Exception("Unknown userid");

        // remove the user id claim so we don't include it as an extra claim if/when we provision the user
        var claims = externalUser.Claims.ToList();
        //claims.Remove(userIdClaim);

        var provider = result.Properties?.Items["scheme"] ?? "";
        var providerUserId = userIdClaim.Value;

        // find external user
        var user = await _users.FindByIdAsync(userIdClaim.Value, CancellationToken.None) ??
                   await _users.FindByNameAsync(externalUser.Identity?.Name!, CancellationToken.None);

        return (user, provider, providerUserId, claims);
    }

    private ApplicationUser? AutoProvisionUser(string provider, string providerUserId, IEnumerable<Claim> claims)
    {
        // Not Implementetd
        return null;
        //var user = _users.AutoProvisionUser(provider, providerUserId, claims.ToList());
        //return user;
    }

    // if the external login is OIDC-based, there are certain things we need to preserve to make logout work
    // this will be different for WS-Fed, SAML2p or other protocols
    private void ProcessLoginCallback(AuthenticateResult externalResult, List<Claim> localClaims, AuthenticationProperties localSignInProps)
    {
        // if the external system sent a session id claim, copy it over
        // so we can use it for single sign-out
        var sid = externalResult.Principal?.Claims.FirstOrDefault(x => x.Type == JwtClaimTypes.SessionId);
        if (sid != null)
        {
            localClaims.Add(new Claim(JwtClaimTypes.SessionId, sid.Value));
        }

        // if the external provider issued an id_token, we'll keep it for signout
        var idToken = externalResult.Properties?.GetTokenValue("id_token");
        if (idToken != null)
        {
            localSignInProps.StoreTokens(new[] { new AuthenticationToken { Name = "id_token", Value = idToken } });
        }
    }
}
