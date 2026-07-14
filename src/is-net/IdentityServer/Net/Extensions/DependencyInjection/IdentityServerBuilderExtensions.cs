using IdentityServerNET.Abstractions.SigningCredential;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Web;
using System;

namespace IdentityServerNET.Extensions.DependencyInjection;

internal static class IdentityServerBuilderExtensions
{
    static public IIdentityServerBuilder AddSigningCredentialsFromCertificateStorage(
            this IIdentityServerBuilder builder,
            IConfiguration configuration)
    {
        builder.Services.AddSigningCredentialCertificateStorage(configuration);

        // Own, isolated ServiceCollection instead of builder.Services: avoids ASP0000
        // (a duplicate copy of every singleton already registered) and is disposed right after use.
        var bootstrapServices = new ServiceCollection();
        bootstrapServices.AddSigningCredentialCertificateStorage(configuration);

        using var bootstrapProvider = bootstrapServices.BuildServiceProvider();
        var certificateStorage = bootstrapProvider.GetRequiredService<ISigningCredentialCertificateStorage>();

        certificateStorage.RenewCertificatesAsync().Wait();
        foreach (var cert in certificateStorage.GetCertificatesAsync().Result)
        {
            builder.AddSigningCredential(cert);
        }

        return builder;
    }

    static public IIdentityServerBuilder AddExternalIdentityProviders(
            this IIdentityServerBuilder builder,
            IConfiguration configuration)
    {
        if (!String.IsNullOrEmpty(configuration["IdentityServer:External:MicrosoftIdentityWeb:ClientId"]))
        {
            builder.Authentication
                .AddMicrosoftIdentityWebApp(options =>
                {
                    options.Instance = "https://login.microsoftonline.com/";
                    options.Domain = configuration["IdentityServer:External:MicrosoftIdentityWeb:Domain"];
                    options.TenantId = configuration["IdentityServer:External:MicrosoftIdentityWeb:TenantId"];
                    options.ClientId = configuration["IdentityServer:External:MicrosoftIdentityWeb:ClientId"];
                    options.ClientSecret = configuration["IdentityServer:External:MicrosoftIdentityWeb:ClientSecret"];
                    options.CallbackPath = "/signin-oidc";
                    options.SignedOutCallbackPath = "";
                },
                openIdConnectScheme: $"{IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread",
                cookieScheme: $"{IdentityServer4.IdentityServerConstants.ExternalCookieAuthenticationScheme}.azuread.cookie",
                displayName: configuration["IdentityServer:External:MicrosoftIdentityWeb:Name"] ?? "Microsoft Identity");
        }

        return builder;
    }
}
