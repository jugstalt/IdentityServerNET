using IdentityServerNET.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using IdentityServer4;

namespace IdentityServerNET.ServerExtension.Default;

[IdentityServerStartup]
public class TestHostingStartup : IIdentityServerStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration, IIdentityServerBuilder identityServerBuilder)
    {
        // Add your custom services here

        //identityServerBuilder.Authentication
        //    .AddMicrosoftIdentityWebApp(options =>
        //    {
        //        options.Instance = "https://login.microsoftonline.com/";
        //        options.Domain = configuration["IdentityServer:External:AzureAD:Domain"];
        //        options.TenantId = configuration["IdentityServer:External:AzureAD:TenantId"];
        //        options.ClientId = configuration["IdentityServer:External:AzureAD:ClientId"];
        //        options.ClientSecret = configuration["IdentityServer:External:AzureAD:ClientSecret"];
        //        options.CallbackPath = "/signin-oidc";
        //        options.SignedOutCallbackPath = "";
        //    },
        //    openIdConnectScheme: $"{IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread",
        //    cookieScheme: $"{IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread.cookie",
        //    displayName: "AzureAD");
    }
}
