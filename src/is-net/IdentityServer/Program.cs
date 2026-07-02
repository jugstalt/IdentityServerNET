using IdentityServer.Net.Extensions.DependencyInjection;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using IdentityServerNET;
using IdentityServerNET.Authorization;
using IdentityServerNET.Abstractions.SigningCredential;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Extensions;
using IdentityServerNET.Extensions.DependencyInjection;
using IdentityServerNET.Models;
using IdentityServerNET.Services;
using IdentityServerNET.Services.SecretsVault;
using IdentityServerNET.Services.Signing;
using IdentityServerNET.Services.UI;
using IdentityServerNET.Services.Validators;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#region Serilog

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate)
    .CreateLogger();

#endregion

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Host.UseSerilog();

#region Custom App config

var customAppConfig =
    args.FirstOrDefault(arg => arg.StartsWith("--customAppSettings="))?.Split('=')[1]
    ?? Environment.GetEnvironmentVariable("IDENTITY_SERVER_CUSTOM_APPSETTINGS");

if (!string.IsNullOrEmpty(customAppConfig))
{
    string customAppConfigFile = $"appsettings.{customAppConfig}.json";
    Log.Logger.Information($"Using custom app config file: {customAppConfigFile} ({(System.IO.File.Exists(customAppConfigFile) ? "exits" : "not exist")})");
    builder.Configuration.AddJsonFile(customAppConfigFile, optional: true, reloadOnChange: false);
}

#endregion

#region _config/...json File

var settingsPrefix = Environment.GetEnvironmentVariable("IDENTITY_SERVER_SETTINGS_PREFIX");

if (string.IsNullOrEmpty(settingsPrefix))
{
    settingsPrefix = "default";
}

var configFile = $"_config/{settingsPrefix}.identityserver.net.json";
Log.Logger.Information($"Using config file: {configFile} ({(System.IO.File.Exists(configFile) ? "exits" : "not exist")})");
builder.Configuration.AddJsonFile(configFile,
                                  optional: true,
                                  reloadOnChange: false);

#endregion

builder.Configuration.ApplyConstants();

#region Data Protection

builder.Services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration.DataProtectionKeysPath()))
                .SetApplicationName("IdentityServer")
                .SetDefaultKeyLifetime(TimeSpan.FromDays(30));
                //.ProtectKeysWithCertificate("thumbprint")  // todo
                ;

#endregion

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
                    options.SignIn.RequireConfirmedAccount = true
        )
    .AddRoles<ApplicationRole>()
    .AddDefaultTokenProviders();

if (builder.Configuration.AllowPasskeyPasswordless() || builder.Configuration.AllowPasskeySecondFactor())
{
    builder.Services.Configure<IdentityPasskeyOptions>(options =>
    {
        var configuredDomain = builder.Configuration.PasskeyServerDomain();
        if (!string.IsNullOrEmpty(configuredDomain))
            options.ServerDomain = configuredDomain;

        // WebAuthn spec: only the origin's hostname (no scheme, no port) must match the RP ID.
        // The browser always sends scheme+host+port (e.g. "https://localhost:44300"),
        // so we must not compare the full origin string.
        options.ValidateOrigin = ctx =>
        {
            if (!Uri.TryCreate(ctx.Origin, UriKind.Absolute, out var originUri))
                return ValueTask.FromResult(false);

            // e.g. "localhost" from "https://localhost:44300"
            var originHost = originUri.Host;

            // Use the configured RP ID; fall back to the request hostname.
            var rpId = options.ServerDomain;
            if (string.IsNullOrEmpty(rpId))
                rpId = ctx.HttpContext.Request.Host.Host;

            return ValueTask.FromResult(
                string.Equals(originHost, rpId, StringComparison.OrdinalIgnoreCase));
        };
    });
}

builder.Services.AddAuthorization(options =>
{
    if (builder.Environment.IsDevelopment() && !String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:AdminUsername"]))
    {
        ApplicationUserExtensions.SetAdministratorUserName(builder.Environment, builder.Configuration);

        options.AddPolicy("admin-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-user-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-role-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-resource-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-client-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-secretsvault-policy",
           policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-signing-ui-policy",
           policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-createcerts-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
        options.AddPolicy("admin-realm-policy",
            policy => policy.RequireUserName(builder.Configuration["IdentityServer:AdminUsername"]));
    }
    else
    {
        // Realm-delegatable capabilities: satisfied by the global role (system admin) or by the
        // role@realm variant (realm admin). The data is scoped to the same realm by the DbContext
        // decorators / IRealmUserScope.
        options.AddPolicy("admin-policy",
            policy => policy.AddRequirements(new RealmAdminRequirement(
                                         KnownRoles.UserAdministrator,
                                         KnownRoles.RoleAdministrator,
                                         KnownRoles.ResourceAdministrator,
                                         KnownRoles.ClientAdministrator)));
        options.AddPolicy("admin-user-policy",
            policy => policy.AddRequirements(new RealmAdminRequirement(KnownRoles.UserAdministrator)));
        options.AddPolicy("admin-role-policy",
            policy => policy.AddRequirements(new RealmAdminRequirement(KnownRoles.RoleAdministrator)));
        options.AddPolicy("admin-resource-policy",
            policy => policy.AddRequirements(new RealmAdminRequirement(KnownRoles.ResourceAdministrator)));
        options.AddPolicy("admin-client-policy",
            policy => policy.AddRequirements(new RealmAdminRequirement(KnownRoles.ClientAdministrator)));

        // System-level capabilities (signing keys, secrets vault, certificate creation) are never
        // realm-delegated: only the holder of the global role (the system admin) passes. Realm admins
        // hold only role@realm variants, so RequireRole(<global>) excludes them by design.
        options.AddPolicy("admin-secretsvault-policy",
           policy => policy.RequireRole(KnownRoles.SecretsVaultAdministrator));
        options.AddPolicy("admin-signing-ui-policy",
           policy => policy.RequireRole(KnownRoles.SigningAdministrator));
        options.AddPolicy("admin-createcerts-policy",
            policy => policy.RequireRole(KnownRoles.ClientAdministrator));

        // Realm management — system admin only (realm admins never hold the realm-administrator role).
        options.AddPolicy("admin-realm-policy",
            policy => policy.RequireRole(KnownRoles.RealmAdministrator));
    }

    // DoTo: find a policy that never matches!!
    options.AddPolicy("forbidden", policy => policy.RequireRole("")); // "_#_locked_for_everybody_#_"));
});

// Realm-aware authorization: resolves role@realm against the caller's current realm.
builder.Services.AddTransient<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    IdentityServerNET.Authorization.RealmAdminAuthorizationHandler>();

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer-Secrets", options =>
    {
        options.Authority = builder.Configuration["IdentityServer:PublicOrigin"];
        options.RequireHttpsMetadata = false;

        options.Audience = "secrets-vault";
    })
    .AddJwtBearer("Bearer-Signing", options =>
    {
        options.Authority = builder.Configuration["IdentityServer:PublicOrigin"];
        options.RequireHttpsMetadata = false;

        options.Audience = "signing-api";
    });

builder.Services.AddMvc()
            .AddRazorPagesOptions(options =>
            {
                // _forbidden => unknown policy will cause an exception => denies access for everyone!!
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "admin-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/users", builder.Configuration.DenyAdminUsers() ? "_forbidden" : "admin-user-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/roles", builder.Configuration.DenyAdminRoles() ? "_forbidden" : "admin-role-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/resources", builder.Configuration.DenyAdminResources() ? "_forbidden" : "admin-resource-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/clients", builder.Configuration.DenyAdminClients() ? "_forbidden" : "admin-client-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/secretsvault", builder.Configuration.DenyAdminSecretsVault() ? "_forbidden" : "admin-secretsvault-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/signing", builder.Configuration.DenySigningUI() ? "_forbidden" : "admin-signing-ui-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/certificates", builder.Configuration.DenyAdminCreateCerts() ? "_forbidden" : "admin-createcerts-policy");
                if (builder.Configuration.DenyManageAccount() == true)
                {
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage", "_forbidden");
                }

                options.Conventions.AuthorizePage("/Account/Login");
                options.Conventions.AuthorizeAreaPage("/Account/Login", "/Account/Login");
            });

builder.Services.AddControllersWithViews().AddNewtonsoftJson();
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        //options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    });

builder.Services.AddTransient<SecretsVaultManager>();

var identityServerBuilder = builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;
    options.UserInteraction.LoginUrl = "/Account/Login";
    options.UserInteraction.LogoutUrl = "/Account/Logout";
    options.Authentication = new AuthenticationOptions()
    {
        CookieLifetime = TimeSpan.FromHours(10), // ID server cookie timeout set to 10 hours
        CookieSlidingExpiration = true,
    };

    if (builder.Configuration["IdentityServer:Endpoints:EnablePushedAuthorization"] is string parEnabled)
        options.Endpoints.EnablePushedAuthorizationEndpoint = !parEnabled.Equals("false", StringComparison.OrdinalIgnoreCase);

    // use ForwaredHeaders: https://github.com/IdentityServer/IdentityServer4/issues/4631
    //if (!String.IsNullOrEmpty(Configuration["IdentityServer:PublicOrigin"]))
    //{
    //    options.PublicOrigin = Configuration["IdentityServer:PublicOrigin"];
    //}
})
    // Add Jwt Client Assertation (get token from certificate)
    .AddSecretParser<JwtBearerClientAssertionSecretParser>()
    .AddSecretValidator<PrivateKeyJwtSecretValidator>()   // Requires IReplayCache
    .AddSecretValidator<SecretsVaultSecretValidator>()
    // Add Identity
    .AddAspNetIdentity<ApplicationUser>()
    // Add Strores
    .AddResourceStore<ResourceStore>()
    .AddClientStore<ClientStore>()
    .AddExternalIdentityProviders(builder.Configuration);

builder.Services.AddTransient<IReplayCache, DefaultReplayCache>();
builder.Services.AddTransient<IAuthorizationContextService, AuthorizationContextService>();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor | 
        ForwardedHeaders.XForwardedProto | 
        ForwardedHeaders.XForwardedHost;
    // obsolote: https://aka.ms/aspnet/deprecate/005
    //options.KnownNetworks.Clear();
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddSingleton<IEventSink, EventSinkProxy>();

if (builder.Configuration.GetSection("IdentityServer:Cookie").GetChildren().Count() > 0 
    || !String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:PublicOrigin"]))
{
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";

        if (!String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:PublicOrigin"]))
        {
            var publicOrigin = new Uri(builder.Configuration["IdentityServer:PublicOrigin"]);

            if (publicOrigin.Scheme == "http")
            {
                Log.Logger.Warning($"Public Origin Scheme is HTTP ({publicOrigin}). Cookie SecurePolicy is set to 'Always'!");
                options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
            }
        }

        if (!String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:Cookie:Name"]))
        {
            options.Cookie.Name = builder.Configuration["IdentityServer:Cookie:Name"];
        }

        if (!String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:Cookie:Domain"]))
        {
            options.Cookie.Domain = builder.Configuration["IdentityServer:Cookie:Domain"];
        }

        if (!String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:Cookie:Path"]))
        {
            options.Cookie.Path = builder.Configuration["IdentityServer:Cookie:Path"];
        }

        if (!String.IsNullOrWhiteSpace(builder.Configuration["IdentityServer:Cookie:ExpireDays"]))
        {
            options.ExpireTimeSpan = TimeSpan.FromDays(int.Parse(builder.Configuration["IdentityServer:Cookie:ExpireDays"]));
        }
    });
}

builder.Services.AddCryptoServices(builder.Configuration);

#region Register Certificate Store

builder.Services.AddSigningCredentialCertificateStorage(builder.Configuration);

#region Refresh Certificate Store and add SigningCredentials

var sp = builder.Services.BuildServiceProvider();
var signingCredentialCertificateStorage = sp.GetService<ISigningCredentialCertificateStorage>();
signingCredentialCertificateStorage.RenewCertificatesAsync().Wait();
foreach (var cert in signingCredentialCertificateStorage.GetCertificatesAsync().Result)
{
    identityServerBuilder.AddSigningCredential(cert);
    //builder.AddValidationKey(cert);
    //break;
}

#endregion

#endregion

builder.Services
            .AddTransient<IEmailSender, EmailSenderProxy>()
            .AddScoped<CustomTokenService>()
            .AddTransient<SetupService>()
            .AddTransient<IRealmProvisioningService, RealmProvisioningService>()
            .AddColorSchemeService();

builder.Services
    .AddUserStore()
    .AddRoleStore()
    .AddServicesFromConfiguration(builder.Configuration)
    .ConfigureCustomStartup(builder.Configuration, identityServerBuilder)
    .AddFallbackServices(builder.Configuration);

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddTransient<DevMigrationService>();
}

var app = builder.Build();

app.MapDefaultEndpoints();


var setupService = app.Services.GetService<SetupService>();
var userInterface = app.Services.GetService<IUserInterfaceService>();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
}

#region UserInterface (Styling)

var uiCustomization = app.Services.GetRequiredService<UICustomizationService>();
uiCustomization.Initialize(userInterface?.OverrideCssContent ?? string.Empty);

#endregion

#region Optional Middleware

if (builder.Configuration["IdentityServer:AddXForwardedProtoMiddleware"] == "true")
{
    Log.Logger.Information("Using XForwardedProtoMiddleware");
    app.AddXForwardedProtoMiddleware();
}

if (builder.Configuration["IdentityServer:UseHttpsRedirection"] != "false")
{
    Log.Logger.Information("Using HttpsRedirection middleware");
    app.UseHttpsRedirection();
}

#endregion

app.UseForwardedHeaders();

app.UseIdentityServerAppBasePath();
app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();

// uncomment, if you want to add MVC
app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// UI customization endpoints — served from in-memory cache, no filesystem required
app.MapGet("/ui/overrides.css", async (UICustomizationService svc) =>
    Results.Content(await svc.GetOverrideCssAsync(), "text/css"))
    .AllowAnonymous();

app.MapGet("/ui/logo", async (UICustomizationService svc) =>
{
    var result = await svc.GetLogoAsync();
    return result is null ? Results.NotFound() : Results.File(result.Value.data, result.Value.mime);
}).AllowAnonymous();

app.MapGet("/ui/background/{index:int}", async (int index, UICustomizationService svc) =>
{
    var result = await svc.GetBackgroundAsync(index);
    return result is null ? Results.NotFound() : Results.File(result.Value.data, result.Value.mime);
}).AllowAnonymous();

app.MapGet("/ui/client-logo/{clientId}", async (string clientId, IdentityServerNET.Abstractions.DbContext.IClientDbContext clientDb) =>
{
    var client = await clientDb.FindClientByIdAsync(clientId);
    if (client is null || !client.HasLogoImage) return Results.NotFound();
    return Results.File(Convert.FromBase64String(client.LogoBase64!), client.LogoMimeType ?? "image/png");
}).AllowAnonymous();
//app.MapControllerRoute(
//        name: "login",
//        pattern: "Identity/Account/Login",
//        defaults: new { controller = "Account", action = "Login" }
//        );
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}"); ;

//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapRazorPages();
//    endpoints.MapControllerRoute(
//        name: "login",
//        pattern: "Identity/Account/Login",
//        defaults: new { controller = "Account", action = "Login" }
//        );
//    endpoints.MapControllerRoute(
//        name: "default",
//        pattern: "{controller=Home}/{action=Index}/{id?}");

//});

app.Run();

// Exposes the implicitly-generated Program class so the integration-test project
// (IdentityServerNET.Host.Tests) can boot the real host in-memory via
// WebApplicationFactory<Program>. This declaration carries no behavior.
public partial class Program { }