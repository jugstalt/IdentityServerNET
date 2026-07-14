using IdentityServerNET.Abstractions.SigningCredential;
using IdentityServerNET.Extensions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Web;
using System;
using System.IO;

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
        // Only used to guarantee at least one certificate exists before Build() - the actual
        // signing/validation key selection happens dynamically at runtime (see AddSigningCredentialRenewal).
        // Data Protection must use the same key ring as the main app (Program.cs) so a certificate
        // password generated/read here stays decryptable at normal runtime.
        var bootstrapServices = new ServiceCollection();
        // AddLogging() alone registers no provider (log calls would be silently dropped) - a plain
        // console provider is enough for this bootstrap-only phase, which runs before Serilog's own
        // pipeline (wired up on the real host in Program.cs) would even see anything.
        bootstrapServices.AddLogging(loggingBuilder => loggingBuilder.AddConsole());
        bootstrapServices.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(configuration.DataProtectionKeysPath()))
            .SetApplicationName("IdentityServer");
        bootstrapServices.AddSigningCredentialCertificateStorage(configuration);

        using (var bootstrapProvider = bootstrapServices.BuildServiceProvider())
        {
            bootstrapProvider.GetRequiredService<ISigningCredentialCertificateStorage>()
                .RenewCertificatesAsync().Wait();
        }

        // DynamicSigningCredentialStore re-reads certificates from storage on a short cache instead of a
        // frozen startup snapshot, and SigningCredentialRenewalBackgroundService periodically creates new
        // certificates, so long-running instances rotate signing keys without needing a restart.
        builder.Services.AddSigningCredentialRenewal(configuration);

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
