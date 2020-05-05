// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer.Legacy;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Factories;
using IdentityServer.Legacy.Services;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.SigningCredential;
using IdentityServer4;
using IdentityServer4.Configuration;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IdentityServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }

        public IWebHostEnvironment Environment { get; }

        public Startup(IWebHostEnvironment environment)
        {
            Environment = environment;
        }

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
                }

            // DoTo: find a policy that never matches!!
            options.AddPolicy("forbidden", policy => policy.RequireRole("")); // "_#_locked_for_everybody_#_"));
            });

            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "https://localhost:44300";
                    options.RequireHttpsMetadata = false;

                    options.Audience = "secrets-vault";
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


            //services.AddTransient<IProfileService, LegacyProfileService>();
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
            })
            // Add Jwt Client Assertation (get token from certificate)
            .AddSecretParser<JwtBearerClientAssertionSecretParser>()
            .AddSecretValidator<PrivateKeyJwtSecretValidator>()
            // Add Identity
            .AddAspNetIdentity<ApplicationUser>()
            //.AddProfileService<LegacyProfileService>()
            // Add Strores
            .AddResourceStore<ResourceStore>()
            .AddClientStore<ClientStore>();

            if (Configuration.GetSection("IdentityServer:Cookie").GetChildren().Count() > 0)
            {
                services.ConfigureApplicationCookie(options =>
                {
                    if(!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Name"]))
                         options.Cookie.Name = Configuration["IdentityServer:Cookie:Name"];
                    if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Domain"]))
                        options.Cookie.Domain = Configuration["IdentityServer:Cookie:Domain"];
                    if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:Path"]))
                        options.Cookie.Path = Configuration["IdentityServer:Cookie:Path"];
                    if (!String.IsNullOrWhiteSpace(Configuration["IdentityServer:Cookie:ExpireDays"]))
                        options.ExpireTimeSpan = TimeSpan.FromDays(int.Parse(Configuration["IdentityServer:Cookie:ExpireDays"]));
                });
            }

            services.AddTransient<ICertificateFactory, CertificateFactory>();

            if (String.IsNullOrEmpty(Configuration["SigningCredential:Storage"]))
            {
                // not recommended for production - you need to store your key material somewhere secure
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                services.AddTransient<ISigningCredentialCertificateStorage, SigningCredentialCertificateStorage>();


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
            }

            services.AddTransient<IEmailSender, EmailSenderProxy>();
        }

        public void Configure(IApplicationBuilder app, IOptions<UserInterfaceConfiguration> userInterfaceConfig = null)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            #region UserInterface (Styling)

            try
            {
                var overrideCss = userInterfaceConfig?.Value?.OverrideCssContent ?? String.Empty;

                FileInfo fi = new FileInfo($"{ Environment.WebRootPath }/css/is4-overrides.css");
                if (fi.Exists)
                {
                    fi.Delete();
                }
                File.WriteAllText(fi.FullName, overrideCss);

                if(userInterfaceConfig?.Value?.MediaContent!=null)
                {
                    foreach(var media in userInterfaceConfig.Value.MediaContent)
                    {
                        fi = new FileInfo($"{ Environment.WebRootPath }/css/media/{ media.Key }");
                        if(!fi.Directory.Exists)
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
            catch (Exception ex)
            {
                Console.WriteLine("Exception: Styling overrrides not updated");
            }

            #endregion

            // uncomment if you want to add MVC
            app.UseHttpsRedirection();
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
}
