using Aspire.Hosting.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspire.Hosting;

static public class IdentityServerNovaMigrationBuilderExtensions
{
    private const string MigEnvPrefix = "IdentityServer__Migrations__";

    public static IdentityServerNovaMigrationBuilder AddAdminPassword(
            this IdentityServerNovaMigrationBuilder builder,
            string adminPassword)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}AdminPassword", adminPassword);
        });

        return builder;
    }

    #region IdentityResources

    public static IdentityServerNovaMigrationBuilder WithIdentityResouce(
            this IdentityServerNovaMigrationBuilder builder,
            string identityResouce)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}IdentityResources__{builder.MigIdentityResourceIndex++}__Name", identityResouce.ToLower());
        });

        return builder;
    }

    public static IdentityServerNovaMigrationBuilder AddIdentityResources(
            this IdentityServerNovaMigrationBuilder builder,
            IEnumerable<string> identityResources
        )
    {
        identityResources?.ToList().ForEach(r => builder.WithIdentityResouce(r));

        return builder;
    }

    #endregion

    #region ApiResources

    public static IdentityServerNovaMigrationBuilder AddApiResource(
            this IdentityServerNovaMigrationBuilder builder,
            string name, IEnumerable<string>? scopes = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}ApiResources__{builder.MigApiResourceIndex}__Name", name.ToLower());

            int index = 0;
            foreach (var scope in scopes ?? [])
            {
                e.EnvironmentVariables.Add($"{MigEnvPrefix}ApiResources__{builder.MigApiResourceIndex}__Scopes__{index++}__Name", scope.ToLower());
            }

            builder.MigApiResourceIndex++;
        });

        return builder;
    }

    #endregion

    #region Roles

    public static IdentityServerNovaMigrationBuilder WithUserRole(
            this IdentityServerNovaMigrationBuilder builder,
            string role)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Roles__{builder.MigApiUserRoleIndex++}__Name", role.ToLower());
        });

        return builder;
    }

    public static IdentityServerNovaMigrationBuilder AddUserRoles(
            this IdentityServerNovaMigrationBuilder builder,
            IEnumerable<string>? userRoles)
    {
        userRoles?.ToList().ForEach(r => builder.WithUserRole(r));

        return builder;
    }

    #endregion

    #region Users

    public static IdentityServerNovaMigrationBuilder WithUser(
            this IdentityServerNovaMigrationBuilder builder,
            string username,
            string password,
            IEnumerable<string>? roles)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{builder.MigApiUserIndex}__Name", username.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{builder.MigApiUserIndex}__Password", password);

            int index = 0;
            foreach (var role in roles ?? [])
            {
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{builder.MigApiUserIndex}__Roles__{index++}", role.ToString());
            }

            builder.MigApiUserIndex++;
        });

        return builder;
    }

    #endregion

    #region Clients

    public static IdentityServerNovaMigrationBuilder WithClient(
            this IdentityServerNovaMigrationBuilder builder,
            ClientType clientType,
            string clientId,
            string clientSecret,
            string? clientUrl = null,
            IEnumerable<string>? scopes = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientType", clientType.ToString());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientId", clientId.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientSecret", clientSecret);

            if (!String.IsNullOrEmpty(clientUrl))
            {
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientUrl", clientUrl);
            }

            int index = 0;
            foreach (var scope in scopes ?? [])
            {
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__Scopes__{index++}", scope.ToLower());
            }
        });

        return builder;
    }

    //IResourceWithEndpoints
    public static IdentityServerNovaMigrationBuilder AddClient(
            this IdentityServerNovaMigrationBuilder builder,
            ClientType clientType,
            string clientId,
            string clientSecret,
            IResourceWithEndpoints resource,
            IEnumerable<string>? scopes = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientType", clientType.ToString());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientId", clientId.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientSecret", clientSecret);

            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__ClientUrl",
                resource.GetEndpoint("https")?.Url ?? "");


            int index = 0;
            foreach (var scope in scopes ?? [])
            {
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{builder.MigClientIndex}__Scopes__{index++}", scope.ToLower());
            }

            builder.MigClientIndex++;
        });

        return builder;
    }

    #endregion
}
