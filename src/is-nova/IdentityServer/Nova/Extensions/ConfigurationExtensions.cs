using Microsoft.Extensions.Configuration;

namespace IdentityServer.Nova.Extensions;

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
}
