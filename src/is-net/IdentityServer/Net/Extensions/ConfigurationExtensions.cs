using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServerNET.Extensions;

static public class ConfigurationExtensions
{
    static public bool DenyAdminUsers(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenyAdminUsers"]?.ToLower() == "true";
    }

    static public bool DenyAdminRoles(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenyAdminRoles"]?.ToLower() == "true";
    }

    static public bool DenyAdminResources(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenyAdminResources"]?.ToLower() == "true";
    }

    static public bool DenyAdminClients(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenyAdminClients"]?.ToLower() == "true";
    }

    static public bool DenyAdminSecretsVault(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenyAdminSecretsVault"]?.ToLower() == "true";
    }

    static public bool DenySigningUI(this IConfiguration configuration)
    {
        return configuration["identityserver:admin:DenySigningUI"]?.ToLower() == "true";
    }

    static public bool DenyAdminCreateCerts(this IConfiguration configuration)
        => configuration["identityserver:admin:DenyAdminCreateCerts"]?.ToLower() == "true";

    static public bool AllowDataTransfer(this IConfiguration configuration)
        => configuration["identityserver:Admin:AllowDataTransfer"]?.ToLower() == "true";


    static public bool DenyManageAccount(this IConfiguration configuration)
    {
        return configuration["identityserver:Account:DenyManageAccount"]?.ToLower() == "true";
    }

    static public bool DenyRegisterAccount(this IConfiguration configuration)
    {
        return configuration["identityserver:Account:DenyRegisterAccount"]?.ToLower() == "true";
    }

    static public bool DenyForgotPasswordChallange(this IConfiguration configuration)
    {
        return configuration["identityserver:Login:DenyForgotPasswordChallange"]?.ToLower() == "true";
    }

    static public bool DenyRememberLogin(this IConfiguration configuration)
    {
        return configuration["identityserver:Login:DenyRememberLogin"]?.ToLower() == "true";
    }

    static public bool RememberLoginDefaultValue(this IConfiguration configuration)
    {
        return configuration["identityserver:Login:RememberLoginDefaultValue"]?.ToLower() == "true";
    }

    /// <summary>
    /// Returns true when passkey-based passwordless login is enabled globally.
    /// Configured via <c>identityserver:Login:Passkey:AllowPasswordless = true</c>.
    /// </summary>
    static public bool AllowPasskeyPasswordless(this IConfiguration configuration)
        => configuration["identityserver:Login:Passkey:AllowPasswordless"]?.ToLower() == "true";

    /// <summary>
    /// Returns true when passkeys may serve as a second factor after username/password.
    /// Configured via <c>identityserver:Login:Passkey:AllowSecondFactor = true</c>.
    /// </summary>
    static public bool AllowPasskeySecondFactor(this IConfiguration configuration)
        => configuration["identityserver:Login:Passkey:AllowSecondFactor"]?.ToLower() == "true";

    /// <summary>
    /// The WebAuthn Relying Party ID (RP ID), usually the bare domain name (e.g. "example.com").
    /// Configured via <c>identityserver:Login:Passkey:ServerDomain</c>.
    /// Defaults to an empty string when not set.
    /// </summary>
    static public string PasskeyServerDomain(this IConfiguration configuration)
        => configuration["identityserver:Login:Passkey:ServerDomain"] ?? "";

    /// <summary>
    /// Human-readable name shown to the user during registration.
    /// Configured via <c>identityserver:Login:Passkey:RelyingPartyName</c>.
    /// Defaults to "IdentityServer" when not set.
    /// </summary>
    static public string PasskeyRelyingPartyName(this IConfiguration configuration)
        => configuration["identityserver:Login:Passkey:RelyingPartyName"] ?? "IdentityServer";

    /// <summary>
    /// True only when <c>IdentityServer:PublicOrigin</c> is set and its scheme is literally "http".
    /// Used to decide whether the self-referencing JWT bearer metadata fetch (Bearer-Secrets/
    /// Bearer-Signing) may relax RequireHttpsMetadata - a bare "false" would silently weaken every
    /// deployment with a real HTTPS PublicOrigin for no functional benefit.
    /// </summary>
    static public bool PublicOriginIsHttp(this IConfiguration configuration)
        => Uri.TryCreate(configuration["IdentityServer:PublicOrigin"], UriKind.Absolute, out var publicOriginUri)
           && publicOriginUri.Scheme == Uri.UriSchemeHttp;

    /// <summary>
    /// Returns true when signing certificates should be kept in memory only instead of persisted to
    /// disk. Configured via <c>identityserver:SigningCredential:InMemoryOnly = true</c>. Certificates
    /// are lost on every restart - only meant for testing/development, never production.
    /// </summary>
    static public bool SigningCredentialInMemoryOnly(this IConfiguration configuration)
        => configuration["identityserver:SigningCredential:InMemoryOnly"]?.ToLower() == "true";

    static public IConfiguration SwitchCase(
            this IConfiguration configuration,
            IEnumerable<string> names,
            Action<string> then
        )
    {
        if (names?.Any() != true) return configuration;

        if (configuration is null) return null;

        foreach (var name in names)
        {
            string value = configuration[name];

            if (!String.IsNullOrEmpty(value))
            {
                then(value);

                return null;  // no more switchCases
            }
        }

        return configuration;
    }


    static public IConfiguration SwitchCase(
            this IConfiguration configuration,
            string name,
            Action<string> then)
        => configuration.SwitchCase([name], then);

    static public IConfiguration SwitchSection(
            this IConfiguration configuration,
            string name,
            Action<IConfigurationSection> then)
    {
        if (configuration is null) return null;

        var section = configuration.GetSection(name);

        if (section.Exists())
        {
            then(section);

            return null;  // no more switchCases
        }

        return configuration;
    }

    static public void SwitchDefault(
            this IConfiguration configuration,
            Action then
        )
    {
        if (configuration is null) return;

        then();
    }
}
