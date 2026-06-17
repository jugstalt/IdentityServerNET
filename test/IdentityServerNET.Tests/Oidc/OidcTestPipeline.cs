// Based on IdentityServer4 integration test infrastructure (IdentityServerPipeline).
// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// A self-contained, environment-independent in-memory host for testing the OpenID Connect /
/// OAuth2 protocol layer of IdentityServer. It wires up <see cref="AddIdentityServer"/> with
/// in-memory clients, resources, scopes and test users and a developer signing credential that
/// is NOT persisted to disk, so the same tests run identically on every machine and in CI.
///
/// The pipeline is modelled on the IdentityServer4 integration test infrastructure but trimmed
/// down to the parts required for flow testing and uses plain xUnit assertions only.
/// </summary>
public class OidcTestPipeline
{
    public const string BaseUrl = "https://server";
    public const string LoginPage = BaseUrl + "/account/login";
    public const string ConsentPage = BaseUrl + "/account/consent";
    public const string ErrorPage = BaseUrl + "/home/error";

    public const string DiscoveryEndpoint = BaseUrl + "/.well-known/openid-configuration";
    public const string DiscoveryKeysEndpoint = BaseUrl + "/.well-known/openid-configuration/jwks";
    public const string AuthorizeEndpoint = BaseUrl + "/connect/authorize";
    public const string TokenEndpoint = BaseUrl + "/connect/token";
    public const string RevocationEndpoint = BaseUrl + "/connect/revocation";
    public const string UserInfoEndpoint = BaseUrl + "/connect/userinfo";
    public const string IntrospectionEndpoint = BaseUrl + "/connect/introspect";
    public const string EndSessionEndpoint = BaseUrl + "/connect/endsession";

    public IdentityServerOptions Options { get; set; } = default!;
    public List<Client> Clients { get; set; } = new();
    public List<IdentityResource> IdentityScopes { get; set; } = new();
    public List<ApiResource> ApiResources { get; set; } = new();
    public List<ApiScope> ApiScopes { get; set; } = new();
    public List<TestUser> Users { get; set; } = new();

    public TestServer Server { get; set; } = default!;
    public HttpMessageHandler Handler { get; set; } = default!;

    public BrowserClient BrowserClient { get; set; } = default!;
    public HttpClient BackChannelClient { get; set; } = default!;

    /// <summary>
    /// A controllable clock injected into IdentityServer so that token lifetimes / expiry can be
    /// tested deterministically. Defaults to the (frozen) time at construction.
    /// </summary>
    public TestClock Clock { get; } = new TestClock();

    public event Action<IServiceCollection> OnPreConfigureServices = _ => { };
    public event Action<IServiceCollection> OnPostConfigureServices = _ => { };

    public void Initialize(bool enableLogging = false)
    {
        var builder = new WebHostBuilder();
        builder.ConfigureServices(ConfigureServices);
        builder.Configure(ConfigureApp);

        Server = new TestServer(builder);
        Handler = Server.CreateHandler();

        BrowserClient = new BrowserClient(new BrowserHandler(Handler));
        BackChannelClient = new HttpClient(Handler);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        OnPreConfigureServices(services);

        services.AddIdentityServer(options =>
        {
            Options = options;

            // Align the interaction URLs with the routes mapped in ConfigureApp below, so that
            // consent/login/error redirects reach the in-memory stand-in endpoints.
            options.UserInteraction.LoginUrl = "/account/login";
            options.UserInteraction.ConsentUrl = "/account/consent";
            options.UserInteraction.ErrorUrl = "/home/error";

            options.Events = new EventsOptions
            {
                RaiseErrorEvents = true,
                RaiseFailureEvents = true,
                RaiseInformationEvents = true,
                RaiseSuccessEvents = true
            };
        })
        .AddInMemoryClients(Clients)
        .AddInMemoryIdentityResources(IdentityScopes)
        .AddInMemoryApiResources(ApiResources)
        .AddInMemoryApiScopes(ApiScopes)
        .AddTestUsers(Users)
        .AddDeveloperSigningCredential(persistKey: false);

        // Override the default ISystemClock AFTER AddIdentityServer so that IdentityServer resolves
        // the controllable test clock. This makes token lifetime / expiry assertions deterministic.
        services.AddSingleton<ISystemClock>(Clock);

        OnPostConfigureServices(services);
    }

    private void ConfigureApp(IApplicationBuilder app)
    {
        app.UseIdentityServer();

        // Minimal stand-in login endpoint: signs in the configured subject and redirects back.
        app.Map("/account/login", path =>
        {
            path.Run(OnLogin);
        });

        // Minimal stand-in consent endpoint: grants the prepared consent and redirects back.
        app.Map("/account/consent", path =>
        {
            path.Run(OnConsent);
        });

        app.Map("/home/error", path =>
        {
            path.Run(OnError);
        });
    }

    // --- login -------------------------------------------------------------------------------

    public bool LoginWasCalled { get; set; }
    public ClaimsPrincipal? Subject { get; set; }

    private async Task OnLogin(HttpContext ctx)
    {
        LoginWasCalled = true;
        if (Subject != null)
        {
            await ctx.SignInAsync(Subject, new AuthenticationProperties());
            var url = ctx.Request.Query[Options.UserInteraction.LoginReturnUrlParameter].FirstOrDefault();
            if (url != null)
            {
                ctx.Response.Redirect(url);
            }
        }
    }

    // --- consent -----------------------------------------------------------------------------

    public bool ConsentWasCalled { get; set; }
    public ConsentResponse? ConsentResponse { get; set; }

    private async Task OnConsent(HttpContext ctx)
    {
        ConsentWasCalled = true;
        var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
        var request = await interaction.GetAuthorizationContextAsync(ctx.Request.Query["returnUrl"].FirstOrDefault());
        if (request != null && ConsentResponse != null)
        {
            await interaction.GrantConsentAsync(request, ConsentResponse);
            var url = ctx.Request.Query[Options.UserInteraction.ConsentReturnUrlParameter].FirstOrDefault();
            if (url != null)
            {
                ctx.Response.Redirect(url);
            }
        }
    }

    // --- error -------------------------------------------------------------------------------

    public bool ErrorWasCalled { get; set; }
    public ErrorMessage? ErrorMessage { get; set; }

    private async Task OnError(HttpContext ctx)
    {
        ErrorWasCalled = true;
        var interaction = ctx.RequestServices.GetRequiredService<IIdentityServerInteractionService>();
        ErrorMessage = await interaction.GetErrorContextAsync(ctx.Request.Query["errorId"].FirstOrDefault());
    }

    // --- helpers -----------------------------------------------------------------------------

    /// <summary>Establishes an authenticated session cookie for the given subject id.</summary>
    public async Task LoginAsync(string subjectId)
    {
        var old = BrowserClient.AllowAutoRedirect;
        BrowserClient.AllowAutoRedirect = false;

        Subject = new IdentityServerUser(subjectId).CreatePrincipal();
        await BrowserClient.GetAsync(LoginPage);

        BrowserClient.AllowAutoRedirect = old;
    }

    public void RemoveLoginCookie()
    {
        BrowserClient.RemoveCookie(BaseUrl, IdentityServerConstants.DefaultCookieAuthenticationScheme);
    }
}

/// <summary>
/// A cookie- and redirect-aware <see cref="HttpClient"/> for exercising interactive (browser)
/// flows against the in-memory <see cref="TestServer"/>.
/// </summary>
public class BrowserClient : HttpClient
{
    public BrowserClient(BrowserHandler browserHandler)
        : base(browserHandler)
    {
        BrowserHandler = browserHandler;
    }

    public BrowserHandler BrowserHandler { get; }

    public bool AllowCookies
    {
        get => BrowserHandler.AllowCookies;
        set => BrowserHandler.AllowCookies = value;
    }

    public bool AllowAutoRedirect
    {
        get => BrowserHandler.AllowAutoRedirect;
        set => BrowserHandler.AllowAutoRedirect = value;
    }

    public Cookie? GetCookie(string uri, string name) => BrowserHandler.GetCookie(uri, name);

    public void RemoveCookie(string uri, string name) => BrowserHandler.RemoveCookie(uri, name);
}

/// <summary>
/// A controllable <see cref="ISystemClock"/> for deterministic token lifetime / expiry tests.
/// The time is frozen at construction and only moves when a test advances it explicitly.
/// </summary>
public class TestClock : ISystemClock
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Advances the (frozen) clock by the given amount.</summary>
    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
}
