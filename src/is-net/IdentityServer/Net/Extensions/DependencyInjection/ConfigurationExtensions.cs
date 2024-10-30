using Microsoft.Extensions.Configuration;
using System;

namespace IdentityServer.Net.Extensions.DependencyInjection;

static public class ConfigurationExtensions
{
    static public IConfiguration ApplyConstants(this IConfiguration configuration)
    {
        AccountOptions.AllowLocalLogin =
            !"true".Equals(configuration["IdentityServer:Login:DenyLocalLogin"], StringComparison.OrdinalIgnoreCase);

        return configuration;
    }
}
