using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy
{
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
    }
}
