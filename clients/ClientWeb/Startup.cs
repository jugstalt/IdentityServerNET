using IdentityServer.Nova.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClientWeb;

public class Startup
{
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        => (_webHostEnvironment, _configuration) = (webHostEnvironment, configuration);

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services)
    {
        services.OpenIdConnectAuthentication(_configuration);

        //services.AddIdentity<IdentityUser, IdentityRole>()
        //            .AddDefaultTokenProviders();

        //services.AddAuthentication(options =>
        //{
        //    options.DefaultScheme = "Cookies";
        //    options.DefaultChallengeScheme = "oidc";
        //})
        //    .AddCookie("Cookies")
        //    .AddOpenIdConnect("oidc", options =>
        //{
        //    options.Authority = "https://localhost:44300";
        //    options.RequireHttpsMetadata = false;

        //    options.ClientId = "mvc";
        //    options.ClientSecret = "secret";
        //    options.ResponseType = "code";

        //    options.GetClaimsFromUserInfoEndpoint = true;

        //    options.SaveTokens = true;

        //    options.Scope.Clear();
        //    //options.Scope.Add("api1");
        //    options.Scope.Add("openid");
        //    options.Scope.Add("profile");
        //    options.Scope.Add("role");
        //    //options.Scope.Add("email");
        //    //options.Scope.Add("role");
        //    //options.Scope.Add("phone");
        //    //options.Scope.Add("address");
        //    //options.Scope.Add("offline_access");

        //    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
        //    {
        //        NameClaimType = "name",
        //        RoleClaimType = "role"
        //    };

        //    options.ClaimActions.MapAllExcept("iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash");

        //    //options.ClaimActions.MapUniqueJsonKey("sub", "sub");
        //    //options.ClaimActions.MapUniqueJsonKey("name", "name");
        //    //options.ClaimActions.MapUniqueJsonKey("given_name", "given_name");
        //    //options.ClaimActions.MapUniqueJsonKey("family_name", "family_name");
        //    //options.ClaimActions.MapUniqueJsonKey("email", "email");
        //});

        services.AddMvc();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute()
                    .RequireAuthorization();
        });
    }
}
