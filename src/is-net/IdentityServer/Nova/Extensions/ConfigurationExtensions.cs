using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
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

    static public IConfiguration SwitchCase(
            this IConfiguration configuration,
            IEnumerable<string> names,
            Action<string> then
        )
    {
        if (names?.Any() != true) return configuration;

        if(configuration is null) return null;

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
        if(configuration is null) return;

        then();
    }
}
