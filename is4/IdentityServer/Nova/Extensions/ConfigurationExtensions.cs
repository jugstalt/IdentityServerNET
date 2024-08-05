using Microsoft.Extensions.Configuration;

namespace IdentityServer.Nova.Extensions;

static public class ConfigurationExtensions
{
    static public bool DenyAdminUsers(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyAdminUsers"]?.ToLower() == "true";
    }

    static public bool DenyAdminRoles(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyAdminRoles"]?.ToLower() == "true";
    }

    static public bool DenyAdminResources(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyAdminResources"]?.ToLower() == "true";
    }

    static public bool DenyAdminClients(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyAdminClients"]?.ToLower() == "true";
    }

    static public bool DenyAdminSecretsVault(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyAdminSecretsVault"]?.ToLower() == "true";
    }

    static public bool DenySigningUI(this IConfiguration configuration)
    {
        return configuration["identityserver:DenySigningUI"]?.ToLower() == "true";
    }

    static public bool DenyManageAccount(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyManageAccount"]?.ToLower() == "true";
    }

    static public bool DenyRegisterAccount(this IConfiguration configuration)
    {
        return configuration["identityserver:DenyRegisterAccount"]?.ToLower() == "true";
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
