using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System;

namespace IdentityServerNET.Extensions.DependencyInjection;

internal static class IdentityServerBuilderExtensions
{
    static public IIdentityServerBuilder AddExternalIdentityProviders(
            this IIdentityServerBuilder builder,
            IConfiguration configuration)
    {
        if (!String.IsNullOrEmpty(configuration["IdentityServer:External:AzureAD:ClientId"]))
        {
            builder.Authentication
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.Domain = configuration["IdentityServer:External:AzureAD:Domain"];
                    options.TenantId = configuration["IdentityServer:External:AzureAD:TenantId"];
                    options.ClientId = configuration["IdentityServer:External:AzureAD:ClientId"];
                    options.ClientSecret = configuration["IdentityServer:External:AzureAD:ClientSecret"];
                    options.CallbackPath = "/signin-oidc";
                    options.SignedOutCallbackPath = "";
                },
                openIdConnectScheme: $"{IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread",
                cookieScheme: $"{IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread.cookie",
                displayName: configuration["IdentityServer:External:AzureAD:Name"] ?? "AzureAD");
        }

        return builder;
    }
}
