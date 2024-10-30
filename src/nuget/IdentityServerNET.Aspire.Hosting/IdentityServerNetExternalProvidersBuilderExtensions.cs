using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Configuration;

namespace Aspire.Hosting;

static public class IdentityServerNetExternalProvidersBuilderExtensions
{
    private const string ExternalPrefix = $"{IdentityServerNetConfigurationBuilderExtensions.ConfigPrefix}External__";

    public static IdentityServerNetExternalProvidersBuilder AddMicrosoftIdentityWeb(
        this IdentityServerNetExternalProvidersBuilder builder,
        IConfigurationSection section)
    {
        string prefix = $"{ExternalPrefix}MicrosoftIdentityWeb__";

        builder.ResourceBuilder.WithEnvironment(e =>
        {
            if (!string.IsNullOrEmpty(section["ClientId"]))
            {
                e.EnvironmentVariables.Add($"{prefix}Name", section["Name"] ?? "");
                e.EnvironmentVariables.Add($"{prefix}Domain", section["Domain"] ?? "");
                e.EnvironmentVariables.Add($"{prefix}TenantId", section["TenantId"] ?? "");
                e.EnvironmentVariables.Add($"{prefix}ClientId", section["ClientId"] ?? "");
                e.EnvironmentVariables.Add($"{prefix}ClientSecret", section["ClientSecret"] ?? "");
            }
        });

        return builder;
    }   
}
