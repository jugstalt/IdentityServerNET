// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using IdentityServer.Legacy;
using IdentityServer4.Configuration;
using IdentityServer4.Validation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
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
            //services.AddIdentity<ApplicationUser, ApplicationRole>()
            //    .AddDefaultTokenProviders();

            services.AddAuthorization(options =>
            {
                //options.AddPolicy("admin-policy",
                //        policy => policy.RequireUserName("identityserver-administrator"));
                //if (Environment.IsDevelopment() && !String.IsNullOrWhiteSpace(Configuration["IdentityServer:AdminUsername"]))
                //{
                //    options.AddPolicy("admin-policy",
                //        policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                //}
                //else
                {
                    options.AddPolicy("admin-policy",
                        policy => policy.RequireRole("identityserver-legacy-administrator"));
                }

                if (Environment.IsDevelopment() && !String.IsNullOrWhiteSpace(Configuration["IdentityServer:AdminUsername"]))
                {
                    options.AddPolicy("admin-users-policy",
                        policy => policy.RequireUserName(Configuration["IdentityServer:AdminUsername"]));
                }
                else
                {
                    options.AddPolicy("admin-users-policy",
                        policy => policy.RequireRole("identityserver-legacy-user-administrator"));
                }
            });

            services.AddMvc()
                .AddRazorPagesOptions(options =>
                {
                    options.Conventions.AuthorizeAreaFolder("Admin", "/", "admin-policy");
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
                    CookieSlidingExpiration = true
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

            // not recommended for production - you need to store your key material somewhere secure
            builder.AddDeveloperSigningCredential();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            // uncomment if you want to add MVC
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();

            // uncomment, if you want to add MVC
            app.UseAuthentication();
            app.UseAuthorization();

            // uncomment, if you want to add MVC
            //app.UseEndpoints(endpoints =>
            //{
            //    endpoints.MapDefaultControllerRoute();
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
