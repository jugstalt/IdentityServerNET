using IdentityServer.Nova;
using IdentityServer.Nova.Abstractions.SigningCredential;
using IdentityServer.Nova.Abstractions.UI;
using IdentityServer.Nova.Extensions;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Factories;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services;
using IdentityServer.Nova.Services.SecretsVault;
using IdentityServer.Nova.Services.Signing;
using IdentityServer.Nova.Services.SigningCredential;
using IdentityServer.Nova.Services.Validators;
using IdentityServer4.Configuration;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;

namespace IdentityServer;

public class Startup
{
    public Startup(IConfiguration configuration, IWebHostEnvironment env)
    {
        Configuration = configuration;
        Environment = env;
    }

    public IConfiguration Configuration { get; }

    public IWebHostEnvironment Environment { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddDefaultIdentity<ApplicationUser>(options =>
                    options.SignIn.RequireConfirmedAccount = true
                )
            .AddRoles<ApplicationRole>()
            .AddDefaultTokenProviders();

        services.AddAuthorization(options =>
        {
            if (Environment.IsDevelopment() && !String.IsNullOrWhiteSpace(Configuration["IdentityServer:AdminUsername"]))
            {
                ApplicationUserExtensions.SetAdministratorUserName(Environment, Configuration);

                options.AddPolicy("admin-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-user-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-role-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-resource-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-client-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-secretsvault-policy",
                   policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-signing-ui-policy",
                   policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                options.AddPolicy("admin-createcerts-policy",
                    policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
            }
            else
            {
                options.AddPolicy("admin-policy",
                    policy => policy.RequireRole(KnownRoles.UserAdministrator,
                                                 KnownRoles.RoleAdministrator,
                                                 KnownRoles.ResourceAdministrator,
                                                 KnownRoles.ClientAdministrator));
                options.AddPolicy("admin-user-policy",
                    policy => policy.RequireRole(KnownRoles.UserAdministrator));
                options.AddPolicy("admin-role-policy",
                    policy => policy.RequireRole(KnownRoles.RoleAdministrator));
                options.AddPolicy("admin-resource-policy",
                    policy => policy.RequireRole(KnownRoles.ResourceAdministrator));
                options.AddPolicy("admin-client-policy",
                    policy => policy.RequireRole(KnownRoles.ClientAdministrator));
                options.AddPolicy("admin-secretsvault-policy",
                   policy => policy.RequireRole(KnownRoles.SecretsVaultAdministrator));
                options.AddPolicy("admin-signing-ui-policy",
                   policy => policy.RequireRole(KnownRoles.SigningAdministrator));
                options.AddPolicy("admin-createcerts-policy",
                    policy => policy.RequireRole(KnownRoles.ClientAdministrator));
            }

            // DoTo: find a policy that never matches!!
            options.AddPolicy("forbidden", policy => policy.RequireRole("")); // "_#_locked_for_everybody_#_"));
        });

        services.AddAuthentication("Bearer")
            .AddJwtBearer("Bearer-Secrets", options =>
            {
                options.Authority = Configuration["IdentityServer:PublicOrigin"];
                options.RequireHttpsMetadata = false;

                options.Audience = "secrets-vault";
            })
            .AddJwtBearer("Bearer-Signing", options =>
            {
                options.Authority = Configuration["IdentityServer:PublicOrigin"];
                options.RequireHttpsMetadata = false;

                options.Audience = "signing-api";
            });

        services.AddMvc()
            .AddRazorPagesOptions(options =>
            {
                // _forbidden => unknown policy will cause an exception => denies access for everyone!!
                options.Conventions.AuthorizeAreaFolder("Admin", "/", "admin-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/users", Configuration.DenyAdminUsers() ? "_forbidden" : "admin-user-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/roles", Configuration.DenyAdminRoles() ? "_forbidden" : "admin-role-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/resources", Configuration.DenyAdminResources() ? "_forbidden" : "admin-resource-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/clients", Configuration.DenyAdminClients() ? "_forbidden" : "admin-client-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/secretsvault", Configuration.DenyAdminSecretsVault() ? "_forbidden" : "admin-secretsvault-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/signing", Configuration.DenySigningUI() ? "_forbidden" : "admin-signing-ui-policy");
                options.Conventions.AuthorizeAreaFolder("Admin", "/certificates", Configuration.DenyAdminCreateCerts() ? "_forbidden" : "admin-createcerts-policy");
                if (Configuration.DenyManageAccount() == true)
                {
                    options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage", "_forbidden");
                }

                options.Conventions.AuthorizePage("/Account/Login");
                options.Conventions.AuthorizeAreaPage("/Account/Login", "/Account/Login");
            });

        services.ConfigureApplicationCookie(options =>
        {
            if (!String.IsNullOrWhiteSpace(Configuration["Cookie:Name"]))
            {
                options.Cookie = new Microsoft.AspNetCore.Http.CookieBuilder() { Name = Configuration["Cookie:Name"] };
            }
            options.LoginPath = "/Account/Login";
            options.LogoutPath = "/Account/Logout";
        });

        services.AddControllersWithViews().AddNewtonsoftJson();
        services.AddRazorPages()
            .AddRazorPagesOptions(options =>
            {
                //options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
            });

        services.AddTransient<SecretsVaultManager>();

        var builder = services.AddIdentityServer(options =>
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
        .AddClientStore<ClientStore>();

        builder.Services.AddTransient<IReplayCache, DefaultReplayCache>();
        builder.Services.AddTransient<IAuthorizationContextService, AuthorizationContextService>();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders =
                ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        });


        services.AddSingleton<IEventSink, EventSinkProxy>();

        if (Configuration.GetSection("IdentityServer:Cookie").GetChildren().Count() > 0)
        {
            services.ConfigureApplicationCookie(options =>
            {
                if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Name"]))
                {
                    options.Cookie.Name = Configuration["IdentityServer:Cookie:Name"];
                }

                if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Domain"]))
                {
                    options.Cookie.Domain = Configuration["IdentityServer:Cookie:Domain"];
                }

                if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Path"]))
                {
                    options.Cookie.Path = Configuration["IdentityServer:Cookie:Path"];
                }

                if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:ExpireDays"]))
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(int.Parse(Configuration["IdentityServer:Cookie:ExpireDays"]));
                }
            });
        }

        services.AddCryptoServices(Configuration);
        services.AddTransient<ICertificateFactory, CertificateFactory>();

        #region Register Certificate Store

        if (String.IsNullOrEmpty(Configuration["IdentityServer:SigningCredential:Storage"]))
        {
            // not recommended for production - you need to store your key material somewhere secure
            services.AddSingleton<ISigningCredentialCertificateStorage, SigningCredentialCertificateInMemoryStorage>();
        }
        else
        {
            services.Configure<SigningCredentialCertificateStorageOptions>(storageOptions =>
            {
                storageOptions.Storage = Configuration["IdentityServer:SigningCredential:Storage"];
                storageOptions.CertPassword = Configuration["IdentityServer:SigningCredential:CertPassword"] ?? "Secu4epas3wOrd";
            });
            services.AddTransient<ISigningCredentialCertificateStorage, SigningCredentialCertificateFileSystemStorage>();
        }

        #region Refresh Certificate Store and add SigningCredentials

        var sp = services.BuildServiceProvider();
        var signingCredentialCertificateStorage = sp.GetService<ISigningCredentialCertificateStorage>();
        signingCredentialCertificateStorage.RenewCertificatesAsync().Wait();
        foreach (var cert in signingCredentialCertificateStorage.GetCertificatesAsync().Result)
        {
            builder.AddSigningCredential(cert);
            //builder.AddValidationKey(cert);
            //break;
        }

        #endregion

        #endregion

        services
            .AddTransient<IEmailSender, EmailSenderProxy>()
            .AddScoped<CustomTokenService>()
            .AddTransient<SetupService>()
            .AddColorSchemeService();

        services
            .AddUserStore()
            .AddRoleStore()
            .AddServicesFromConfiguration(Configuration)
            .ConfigureCustomNovaStartup(Configuration)
            .AddFallbackServices(Configuration);
    }

    public void Configure(
                IApplicationBuilder app,
                SetupService setupService = null,
                IUserInterfaceService userInterface = null
        )
    {
        if (Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            Microsoft.IdentityModel.Logging.IdentityModelEventSource.ShowPII = true;
        }

        #region UserInterface (Styling)

        try
        {
            var overrideCss = userInterface?.OverrideCssContent ?? String.Empty;

            FileInfo fi = new FileInfo($"{Environment.WebRootPath}/css/is4-overrides.css");
            if (fi.Exists)
            {
                fi.Delete();
            }
            File.WriteAllText(fi.FullName, overrideCss);

            if (userInterface?.MediaContent != null)
            {
                foreach (var media in userInterface.MediaContent)
                {
                    fi = new FileInfo($"{Environment.WebRootPath}/css/media/{media.Key}");
                    if (!fi.Directory.Exists)
                    {
                        fi.Directory.Create();
                    }
                    if (fi.Exists)
                    {
                        fi.Delete();
                    }
                    File.WriteAllBytes(fi.FullName, media.Value);
                }
            }
        }
        catch /*(Exception ex)*/
        {
            Console.WriteLine("Exception: Styling overrrides not updated");
        }

        #endregion

        app.Use(async (context, next) =>
        {
            var xproto = context.Request.Headers["X-Forwarded-Proto"].ToString();
            if (xproto != null && xproto.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                context.Request.Scheme = "https";
            }
            await next();
        });

        #region Optional Middleware

        if (Configuration["IdentityServer:AddXForwardedProtoMiddleware"] == "true")
        {
            app.AddXForwardedProtoMiddleware();
        }

        if (Configuration["IdentityServer:UseHttpsRedirection"] != "false")
        {
            app.UseHttpsRedirection();
        }

        #endregion

        app.UseForwardedHeaders();

        app.UseStaticFiles();
        app.UseRouting();

        app.UseIdentityServer();

        // uncomment, if you want to add MVC
        app.UseAuthentication();
        app.UseAuthorization();

        // to allow images with base64... eg. captcha images (image-src data:)
        //app.Use(async (ctx, next) =>
        //{
        //    ctx.Response.Headers.Add("Content-Security-Policy",
        //                             "default-src 'self' data:; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';");
        //    await next();
        //});

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages();
            endpoints.MapControllerRoute(
                name: "login",
                pattern: "Identity/Account/Login",
                defaults: new { controller = "Account", action = "Login" }
                );
            endpoints.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

        });
    }
}
