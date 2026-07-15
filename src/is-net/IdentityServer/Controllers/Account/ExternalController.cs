#nullable enable

using Duende.IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.Extensions;

namespace IdentityServer;

[AllowAnonymous]
public class ExternalController : Controller
{
    private readonly IUserStore<ApplicationUser> _users;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly ILogger<ExternalController> _logger;
    private readonly IEventService _events;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IConfiguration _configuration;
    private readonly IRealmDbContext? _realmDb;

    public ExternalController(
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IEventService events,
        ILogger<ExternalController> logger,
        IUserStore<ApplicationUser> users,
        SignInManager<ApplicationUser> signInManager,
        IConfiguration configuration,
        IRealmDbContext? realmDb = null)
    {
        _users = users;

        _interaction = interaction;
        _clientStore = clientStore;
        _logger = logger;
        _events = events;
        _signInManager = signInManager;
        _configuration = configuration;
        _realmDb = realmDb;
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

        var externalUsername = result.Principal.GetUsername();
        var externalEmail = result.Principal.GetEmail() ??
            (
                externalUsername.IsValidEmailAddress()
                ? externalUsername
                : ""
            );

        // lookup our user and external provider info
        var (user, provider, providerUserId, claims) = await FindUserFromExternalProvider(result);
        if (user is null)
        {
            // this might be where you might initiate a custom workflow for user registration
            // in this sample we don't show how that would be done, as our sample implementation
            // simply auto-provisions new external user
            user = AutoProvisionUser(provider, providerUserId, claims);
        }

        if (user is null &&
            !_configuration.DenyRegisterAccount() &&
            (externalUsername.IsValidGeneralUsername() || externalUsername.IsValidEmailAddress()) &&
            externalEmail.IsValidEmailAddress())
        {
            
            var _ = await _users.CreateAsync(new ApplicationUser()
            {
                UserName = externalUsername.ToLower(),
                Email = externalEmail.ToLower(),
                EmailConfirmed = true,
            }, CancellationToken.None);

            user = await _users.FindByNameAsync(externalUsername, CancellationToken.None);
        }

        if (user is null)
        {
            throw new Exception("Can't determine external user");
        }

        // Resolve return URL and IS4 context early so they are available for the realm guard.
        var returnUrl = result.Properties?.Items["returnUrl"] ?? "~/";
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

        // Realm guard — verify the external user is allowed to access this client before signing in.
        if (!await IsUserAllowedForClientAsync(user, context?.Client))
        {
            // Clean up the external auth cookies without issuing an app session cookie.
            await HttpContext.SignOutAsync($"{externalAuthScheme}");
            await HttpContext.SignOutAsync($"{externalAuthScheme}.cookie");

            await _events.RaiseAsync(new UserLoginFailureEvent(user.UserName, "realm access denied", clientId: context?.Client.ClientId));

            if (context != null)
            {
                // Send a proper OAuth error back to the client application.
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                if (context.IsNativeClient())
                    return this.LoadingPage("Redirect", returnUrl);
                return Redirect(returnUrl);
            }
            return RedirectToAction("AccessDenied", "Account");
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

        await _signInManager.SignInAsync(user, localSignInProps);

        // delete temporary cookie used during external authentication
        await HttpContext.SignOutAsync($"{externalAuthScheme}");
        await HttpContext.SignOutAsync($"{externalAuthScheme}.cookie");

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

    private async Task<bool> IsUserAllowedForClientAsync(ApplicationUser user, Client? client)
    {
        if (_realmDb is null || client?.ClientId.HasRealm() != true)
            return true;

        var email = string.IsNullOrEmpty(user.Email) ? user.UserName : user.Email;
        if (string.IsNullOrEmpty(email)) return false;

        int at = email!.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1) return false;

        var domain = email.Substring(at + 1).ToLowerInvariant();
        var realm = await _realmDb.FindByDomainAsync(domain, CancellationToken.None);
        return client.ClientAllowsUser(realm?.Name, domain);
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
