// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Duende.IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Extensions;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace IdentityServer;

/// <summary>
/// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
/// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
/// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
/// </summary>
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IClientStore _clientStore;
    private readonly IAuthenticationSchemeProvider _schemeProvider;
    private readonly IEventService _events;
    private readonly ILoginBotDetection _loginBotDetection;
    private readonly ICaptchaCodeRenderer _captchaCodeRenderer;
    private readonly IConfiguration _configuration;
    private readonly IUserPasskeyStore<ApplicationUser> _passkeyStore;
    private readonly IRealmDbContext _realmDb;

    public AccountController(
        ILogger<AccountController> logger,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityServerInteractionService interaction,
        IClientStore clientStore,
        IAuthenticationSchemeProvider schemeProvider,
        IEventService events,
        IConfiguration configuration,
        IRealmDbContext realmDb = null,
        IUserPasskeyStore<ApplicationUser> passkeyStore = null,
        ILoginBotDetection loginBotDetetion = null,
        ICaptchaCodeRenderer captchaCodeRenderer = null)
    {
        _logger = logger;

        _userManager = userManager;
        _signInManager = signInManager;

        _interaction = interaction;
        _clientStore = clientStore;
        _schemeProvider = schemeProvider;
        _events = events;

        _configuration = configuration;
        _realmDb = realmDb;
        _passkeyStore = passkeyStore;

        _loginBotDetection = loginBotDetetion;
        _captchaCodeRenderer = captchaCodeRenderer;
    }

    /// <summary>
    /// Step 1 — show the identifier (username) form.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Login(string returnUrl, bool forceLocal = false)
    {
        // Pick up any pending error forwarded via TempData from PasskeySignIn or other POST redirects.
        if (TempData["PendingLoginError"] is string pendingError)
            ModelState.AddModelError(string.Empty, pendingError);

        var vm = await BuildIdentifierViewModelAsync(returnUrl, forceLocal);

        if (vm.IsExternalLoginOnly)
            return RedirectToAction("Challenge", "External", new { scheme = vm.ExternalLoginScheme, returnUrl });

        return View(vm);
    }

    /// <summary>
    /// Step 1 POST — validate the identifier, check domain/realm access, then redirect to step 2.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginIdentifierInputModel model, string button)
    {
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        if (button != "identify")
        {
            if (context != null)
            {
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                if (context.IsNativeClient())
                    return this.LoadingPage("Redirect", model.ReturnUrl);
                return Redirect(model.ReturnUrl);
            }
            return Redirect("~/");
        }

        if (ModelState.IsValid)
        {
            // Domain-level realm check — no user lookup, so no username enumeration.
            if (!await IsEmailDomainAllowedForClientAsync(model.Username, context))
            {
                ModelState.AddModelError(string.Empty, "Users from this domain are not permitted to access this application.");
                var vmError = await BuildIdentifierViewModelAsync(model.ReturnUrl);
                vmError.Username = model.Username;
                return View(vmError);
            }

            // Detect user's realm from email domain (for UI on the password page).
            // Always clear first so "Change username" to a non-realm user resets the realm UI.
            TempData.Remove("LoginPendingRealm");
            if (_realmDb is not null)
            {
                var atIdx = model.Username.LastIndexOf('@');
                if (atIdx > 0)
                {
                    var domain = model.Username[(atIdx + 1)..].ToLowerInvariant();
                    var userRealm = await _realmDb.FindByDomainAsync(domain, CancellationToken.None);
                    if (userRealm?.Name is not null)
                        TempData["LoginPendingRealm"] = userRealm.Name;
                }
            }

            // Store username in TempData (encrypted cookie) — keeps it out of the URL and logs.
            TempData["LoginPendingUsername"] = model.Username;
            return RedirectToAction("LoginPassword", new { returnUrl = model.ReturnUrl });
        }

        var vm = await BuildIdentifierViewModelAsync(model.ReturnUrl);
        vm.Username = model.Username;
        return View(vm);
    }

    /// <summary>
    /// Step 2 — show the password form with optional client branding.
    /// Username is read from TempData (encrypted cookie set by Login POST) to keep it out of the URL.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPassword(string returnUrl, bool forceLocal = false)
    {
        // Peek keeps the value alive across page refreshes (does not consume TempData).
        var username = TempData.Peek("LoginPendingUsername") as string;
        if (string.IsNullOrEmpty(username))
            return RedirectToAction("Login", new { returnUrl });

        var vm = await BuildLoginViewModelAsync(returnUrl, forceLocal);
        vm.Username = username;
        vm.AllowRememberLogin = !_configuration.DenyRememberLogin();
        vm.RememberLogin = _configuration.RememberLoginDefaultValue();
        return View(vm);
    }

    /// <summary>
    /// Step 2 POST — authenticate with username + password.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> LoginPassword(LoginInputModel model, string button)
    {
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

        if (button != "login")
        {
            if (context != null)
            {
                await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                if (context.IsNativeClient())
                    return this.LoadingPage("Redirect", model.ReturnUrl);
                return Redirect(model.ReturnUrl);
            }
            return Redirect("~/");
        }

        if (ModelState.IsValid)
        {
            try
            {
                if (String.IsNullOrWhiteSpace(model.Password))
                    throw new Exception("Password is empty");

                var loginUsername = await ResolveLoginUsernameAsync(model.Username);

                bool suspicous = false;
                if (_loginBotDetection != null && await _loginBotDetection.IsSuspiciousUserAsync(model.Username))
                {
                    await _loginBotDetection.BlockSuspicousUser(model.Username);
                    if (_captchaCodeRenderer != null)
                    {
                        if (!await _loginBotDetection.VerifyCaptchaCodeAsync(model.Username, model.CaptchaCode))
                            suspicous = true;
                    }
                }

                var result = suspicous == true
                    ? Microsoft.AspNetCore.Identity.SignInResult.Failed
                    : await _signInManager.PasswordSignInAsync(loginUsername, model.Password, model.RememberLogin, lockoutOnFailure: true);

                if (result.Succeeded)
                {
                    var user = await _userManager.FindByNameAsync(loginUsername);
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName));

                    // Consume pending TempData now that login succeeded.
                    TempData.Remove("LoginPendingUsername");
                    TempData.Remove("LoginPendingRealm");

                    // Realm guard — also enforced here (in addition to step 1) for security.
                    if (!await IsUserAllowedForClientAsync(user, context))
                    {
                        await _signInManager.SignOutAsync();
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "realm access denied", clientId: context?.Client.ClientId));
                        ModelState.AddModelError(string.Empty, "Your account is not permitted to access this application.");
                        var vmDenied = await BuildLoginViewModelAsync(model);
                        return View(vmDenied);
                    }

                    if (_loginBotDetection != null)
                        await _loginBotDetection.RemoveSuspiciousUserAsync(loginUsername);

                    // Passkey second factor.
                    if (_configuration.AllowPasskeySecondFactor()
                        && _passkeyStore != null
                        && (await _userManager.GetPasskeysAsync(user)).Count > 0)
                    {
                        await _signInManager.SignOutAsync();
                        var tfIdentity = new ClaimsIdentity(IdentityConstants.TwoFactorUserIdScheme);
                        tfIdentity.AddClaim(new Claim(ClaimTypes.Name, user.Id));
                        await HttpContext.SignInAsync(IdentityConstants.TwoFactorUserIdScheme, new ClaimsPrincipal(tfIdentity));
                        var encodedReturnUrlPk = HttpUtility.UrlEncode(
                            (context != null || Url.IsLocalUrl(model.ReturnUrl)) ? model.ReturnUrl : "~/");
                        return Redirect(string.Format("~/Identity/Account/LoginWithPasskey?ReturnUrl={0}", encodedReturnUrlPk));
                    }

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                            return this.LoadingPage("Redirect", model.ReturnUrl);
                        return Redirect(model.ReturnUrl);
                    }

                    if (Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);
                    else if (string.IsNullOrEmpty(model.ReturnUrl))
                        return Redirect("~/");
                    else
                        throw new Exception("invalid return URL");
                }
                else if (result.RequiresTwoFactor)
                {
                    if (_loginBotDetection != null)
                        await _loginBotDetection.RemoveSuspiciousUserAsync(loginUsername);

                    var encodedReturnUrl = HttpUtility.UrlEncode(
                        (context != null || Url.IsLocalUrl(model.ReturnUrl)) ? model.ReturnUrl : "~/");

                    if (_configuration.AllowPasskeySecondFactor())
                    {
                        var tfUser = await _signInManager.GetTwoFactorAuthenticationUserAsync();
                        if (tfUser != null && (await _userManager.GetPasskeysAsync(tfUser)).Count > 0)
                        {
                            return Redirect(string.Format(
                                "~/Identity/Account/LoginWithPasskey?ReturnUrl={0}", encodedReturnUrl));
                        }
                    }

                    return Redirect(string.Format("~/Identity/Account/LoginWith2fa?ReturnUrl={0}", encodedReturnUrl));
                }

                if (_loginBotDetection != null)
                {
                    string captcaCode = await _loginBotDetection.AddSuspicousUserAndGenerateCaptchaCodeAsync(model.Username);
                    if (await _loginBotDetection.IsSuspiciousUserAsync(model.Username))
                    {
                        if (!String.IsNullOrEmpty(captcaCode) && _captchaCodeRenderer != null)
                        {
                            byte[] captchaImageBytes = _captchaCodeRenderer.RenderCodeToImage(captcaCode);
                            model.CaptchaImage = captchaImageBytes;
                            this.Response.Headers.Append("Content-Security-Policy",
                                "default-src 'self' data:; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';");
                        }
                    }
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(model.Username, "invalid credentials", clientId: context?.Client.ClientId));
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            }
            catch (StatusMessageException sme)
            {
                ModelState.AddModelError(string.Empty, sme.Message);
                _logger.LogError("Warning on login: {message}", sme.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Sorry, something went wrong...");
                _logger.LogError("Error on login: {message}", ex.Message);
            }
        }

        var vm = await BuildLoginViewModelAsync(model);
        return View(vm);
    }


    /// <summary>
    /// Show logout page
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(string logoutId)
    {
        // build a model so the logout page knows what to display
        var vm = await BuildLogoutViewModelAsync(logoutId);

        if (vm.ShowLogoutPrompt == false)
        {
            // if the request for logout was properly authenticated from IdentityServer, then
            // we don't need to show the prompt and can just log the user out directly.
            return await Logout(vm);
        }

        return View(vm);
    }

    /// <summary>
    /// Handle logout page postback
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(LogoutInputModel model)
    {
        // build a model so the logged out page knows what to display
        var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

        if (User?.Identity.IsAuthenticated == true)
        {
            // delete local authentication cookie
            await _signInManager.SignOutAsync();

            // raise the logout event
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
        }

        // check if we need to trigger sign-out at an upstream identity provider
        if (vm.TriggerExternalSignout)
        {
            // build a return URL so the upstream provider will redirect back
            // to us after the user has logged out. this allows us to then
            // complete our single sign-out processing.
            string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

            // this triggers a redirect to the external provider for sign-out
            return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
        }

        //return View("LoggedOut", vm);

        if (!String.IsNullOrWhiteSpace(vm.PostLogoutRedirectUri))
        {
            return Redirect(vm.PostLogoutRedirectUri);
        }

        //return Redirect("~/Account/Login");
        return Redirect("~/Home/Index");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // -----------------------------------------------------------------------
    // Passkey (WebAuthn) endpoints
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns assertion options JSON for a passwordless passkey sign-in.
    /// Called via fetch() from passkey.js.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> PasskeyChallenge()
    {
        var json = await _signInManager.MakePasskeyRequestOptionsAsync(null);
        return Content(json, "application/json");
    }

    /// <summary>
    /// Verifies the WebAuthn assertion and signs the user in (first-factor passwordless).
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AllowAnonymous]
    public async Task<IActionResult> PasskeySignIn(
        string returnUrl, string assertionJson,
        string button)
    {
        if (button == "login")
        {
            // Fallback: send the user through the identifier-first flow.
            return RedirectToAction("Login", new { returnUrl });
        }

        if (string.IsNullOrWhiteSpace(assertionJson))
        {
            TempData["PendingLoginError"] = "No passkey response received.";
            return RedirectToAction("Login", new { returnUrl });
        }

        var assertResult = await _signInManager.PerformPasskeyAssertionAsync(assertionJson);
        if (!assertResult.Succeeded)
        {
            var reason = assertResult.Failure?.Message ?? "unknown reason";
            _logger.LogWarning("Passkey assertion failed: {Reason}", reason);
            TempData["PendingLoginError"] = $"Passkey sign-in failed: {reason}";
            return RedirectToAction("Login", new { returnUrl });
        }

        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);

        // Realm guard — check before signing in so no session cookie is issued on denial.
        if (!await IsUserAllowedForClientAsync(assertResult.User, context))
        {
            await _events.RaiseAsync(new UserLoginFailureEvent(
                assertResult.User.UserName, "realm access denied", clientId: context?.Client.ClientId));
            TempData["PendingLoginError"] = "Users from this domain are not permitted to access this application.";
            return RedirectToAction("Login", new { returnUrl });
        }

        await _signInManager.SignInAsync(assertResult.User, isPersistent: false);
        await _events.RaiseAsync(new UserLoginSuccessEvent(
            assertResult.User.UserName, assertResult.User.Id, assertResult.User.UserName));

        if (context != null)
            return context.IsNativeClient()
                ? this.LoadingPage("Redirect", returnUrl)
                : Redirect(returnUrl);

        if (Url.IsLocalUrl(returnUrl))
            return Redirect(returnUrl);

        return Redirect("~/");
    }

    /// <summary>
    /// Returns attestation options JSON for registering a new passkey (called from Manage/Passkeys page).
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> PasskeyCreationOptions()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var entity = new PasskeyUserEntity
        {
            Id   = user.Id,
            Name = user.UserName ?? user.Email,
            DisplayName = user.UserName ?? user.Email
        };
        var json = await _signInManager.MakePasskeyCreationOptionsAsync(entity);
        return Content(json, "application/json");
    }

    /// <summary>
    /// Saves a newly registered passkey for the currently authenticated user.
    /// </summary>
    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PasskeyRegister(string attestationJson, string returnUrl = "~/Identity/Account/Manage/Passkeys")
    {
        if (string.IsNullOrWhiteSpace(attestationJson))
        {
            TempData["StatusMessage"] = "Error: No attestation data received.";
            return LocalRedirect(returnUrl);
        }

        var attestResult = await _signInManager.PerformPasskeyAttestationAsync(attestationJson);
        if (!attestResult.Succeeded)
        {
            TempData["StatusMessage"] = "Error: Passkey registration failed – " + attestResult.Failure?.Message;
            return LocalRedirect(returnUrl);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        if (_passkeyStore != null)
        {
            await _passkeyStore.AddOrUpdatePasskeyAsync(
                user, attestResult.Passkey, System.Threading.CancellationToken.None);
        }

        TempData["StatusMessage"] = "Passkey registered successfully.";
        return LocalRedirect(returnUrl);
    }

    /*****************************************/
    /* helper APIs for the AccountController */
    /*****************************************/

    /// <summary>
    /// Returns false when the client is realm-scoped and the email's domain does not map to
    /// that realm. Works without a user record — avoids username enumeration in step 1.
    /// Falls back to true for non-email input or global clients.
    /// </summary>
    private async Task<bool> IsEmailDomainAllowedForClientAsync(string input, AuthorizationRequest context)
    {
        var clientId = context?.Client?.ClientId;
        if (_realmDb is null || !clientId.HasRealm())
            return true;

        if (string.IsNullOrEmpty(input))
            return true;

        int at = input.LastIndexOf('@');
        if (at <= 0 || at == input.Length - 1)
            return true; // not email-shaped — let auth step decide

        var suffix = input.Substring(at + 1).ToLowerInvariant();
        if (!suffix.Contains('.'))
            return true; // realm slug shape, not an email domain — let auth step decide

        var realm = await _realmDb.FindByDomainAsync(suffix, CancellationToken.None);
        return context.Client.ClientAllowsUser(realm?.Name, suffix);
    }

    /// <summary>
    /// Returns false when the client is realm-scoped and the user's email domain does not belong
    /// to that realm. Global clients (no realm suffix) always return true.
    /// </summary>
    private async Task<bool> IsUserAllowedForClientAsync(ApplicationUser user, AuthorizationRequest context)
    {
        var clientId = context?.Client?.ClientId;
        if (_realmDb is null || !clientId.HasRealm())
            return true;

        var email = string.IsNullOrEmpty(user.Email) ? user.UserName : user.Email;
        if (string.IsNullOrEmpty(email))
            return false;

        int at = email!.LastIndexOf('@');
        if (at <= 0 || at == email.Length - 1)
            return false;

        var domain = email.Substring(at + 1).ToLowerInvariant();
        var realm = await _realmDb.FindByDomainAsync(domain, CancellationToken.None);
        return context.Client.ClientAllowsUser(realm?.Name, domain);
    }

    /// <summary>
    /// If the supplied input looks like an email address, attempt to find the user by email
    /// and return their stored UserName. Falls back to the original input if no match is found,
    /// so that the standard "invalid credentials" path is taken rather than a confusing 404.
    /// </summary>
    private async Task<string> ResolveLoginUsernameAsync(string input)
    {
        if (!string.IsNullOrWhiteSpace(input) && input.Contains('@'))
        {
            var user = await _userManager.FindByEmailAsync(input);
            if (user?.UserName != null)
                return user.UserName;
        }
        return input;
    }

    private async Task<LoginIdentifierViewModel> BuildIdentifierViewModelAsync(string returnUrl, bool forceLocal = false)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP != null)
        {
            return new LoginIdentifierViewModel
            {
                EnableLocalLogin = false,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
            };
        }

        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var providers = schemes
            .Where(x => x.DisplayName != null ||
                        x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ExternalProvider { DisplayName = x.DisplayName, AuthenticationScheme = x.Name })
            .ToList();

        var allowLocal = true;
        if (context?.Client.ClientId != null)
        {
            var client = await _clientStore.FindEnabledClientByIdAsync(context.Client.ClientId);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions?.Any() == true)
                    providers = providers.Where(p => client.IdentityProviderRestrictions.Contains(p.AuthenticationScheme)).ToList();
            }
        }

        return new LoginIdentifierViewModel
        {
            EnableLocalLogin = (allowLocal && AccountOptions.AllowLocalLogin) || forceLocal,
            AllowPasskeyLogin = _configuration.AllowPasskeyPasswordless(),
            ReturnUrl = returnUrl,
            Username = context?.LoginHint,
            ExternalProviders = providers.ToArray()
        };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, bool forceLocal = false)
    {
        var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
        if (context?.IdP != null)
        {
            return new LoginViewModel
            {
                EnableLocalLogin = false,
                ReturnUrl = returnUrl,
                Username = context?.LoginHint,
                ExternalProviders = new ExternalProvider[] { new ExternalProvider { AuthenticationScheme = context.IdP } }
            };
        }

        var schemes = await _schemeProvider.GetAllSchemesAsync();
        var providers = schemes
            .Where(x => x.DisplayName != null ||
                        x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
            .Select(x => new ExternalProvider { DisplayName = x.DisplayName, AuthenticationScheme = x.Name })
            .ToList();

        var allowLocal = true;
        string clientId = null;
        string clientName = null;

        if (context?.Client.ClientId != null)
        {
            clientId = context.Client.ClientId;
            clientName = context.Client.ClientName;
            var client = await _clientStore.FindEnabledClientByIdAsync(clientId);
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions?.Any() == true)
                    providers = providers.Where(p => client.IdentityProviderRestrictions.Contains(p.AuthenticationScheme)).ToList();
            }
        }

        return new LoginViewModel
        {
            AllowRememberLogin = AccountOptions.AllowRememberLogin,
            EnableLocalLogin = (allowLocal && AccountOptions.AllowLocalLogin) || forceLocal,
            AllowPasskeyLogin = _configuration.AllowPasskeyPasswordless(),
            ReturnUrl = returnUrl,
            Username = context?.LoginHint,
            ExternalProviders = providers.ToArray(),
            ClientId = clientId,
            ClientName = clientName,
        };
    }

    private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
    {
        var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
        vm.Username = model.Username;
        vm.RememberLogin = model.RememberLogin;
        vm.CaptchaCode = model.CaptchaCode;
        vm.CaptchaImage = model.CaptchaImage;
        return vm;
    }

    private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
    {
        var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

        if (User?.Identity.IsAuthenticated != true)
        {
            // if the user is not authenticated, then just show logged out page
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        var context = await _interaction.GetLogoutContextAsync(logoutId);
        if (context?.ShowSignoutPrompt == false)
        {
            // it's safe to automatically sign-out
            vm.ShowLogoutPrompt = false;
            return vm;
        }

        // show the logout prompt. this prevents attacks where the user
        // is automatically signed out by another malicious web page.
        return vm;
    }

    private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
    {
        // get context information (client name, post logout redirect URI and iframe for federated signout)
        var logout = await _interaction.GetLogoutContextAsync(logoutId);

        var vm = new LoggedOutViewModel
        {
            AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl,
            LogoutId = logoutId
        };

        if (User?.Identity.IsAuthenticated == true)
        {
            var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
            {
                var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                if (providerSupportsSignout)
                {
                    if (vm.LogoutId == null)
                    {
                        // if there's no current logout context, we need to create one
                        // this captures necessary info from the current logged in user
                        // before we signout and redirect away to the external IdP for signout
                        vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                    }

                    vm.ExternalAuthenticationScheme = idp;
                }
            }
        }

        return vm;
    }
}
