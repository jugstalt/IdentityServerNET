using Aspire.Hosting.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Aspire.Hosting;

static public class IdentityServerNetMigrationBuilderExtensions
{
    private const string MigEnvPrefix = $"{IdentityServerNetConfigurationBuilderExtensions.ConfigPrefix}Migrations__";

    #region Admin password

    public static IdentityServerNetMigrationBuilder AddAdminPassword(
            this IdentityServerNetMigrationBuilder builder,
            string adminPassword)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
            e.EnvironmentVariables.Add($"{MigEnvPrefix}AdminPassword", adminPassword));

        return builder;
    }

    #endregion

    #region Identity resources

    public static IdentityServerNetMigrationBuilder WithIdentityResource(
            this IdentityServerNetMigrationBuilder builder,
            string identityResource)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
            e.EnvironmentVariables.Add(
                $"{MigEnvPrefix}IdentityResources__{builder.MigIdentityResourceIndex++}__Name",
                identityResource.ToLower()));

        return builder;
    }

    public static IdentityServerNetMigrationBuilder AddIdentityResources(
            this IdentityServerNetMigrationBuilder builder,
            IEnumerable<string> identityResources)
    {
        foreach (var r in identityResources ?? [])
            builder.WithIdentityResource(r);

        return builder;
    }

    #endregion

    #region API resources

    /// <summary>
    /// Adds an API resource. Scopes are created as "{name}.{scope}" sub-scopes.
    /// The resource-level scope "{name}" is always added automatically.
    /// </summary>
    public static IdentityServerNetMigrationBuilder AddApiResource(
            this IdentityServerNetMigrationBuilder builder,
            string name,
            IEnumerable<string>? scopes = null,
            string? apiSecret = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            int i = builder.MigApiResourceIndex;
            e.EnvironmentVariables.Add($"{MigEnvPrefix}ApiResources__{i}__Name", name.ToLower());

            if (!string.IsNullOrEmpty(apiSecret))
                e.EnvironmentVariables.Add($"{MigEnvPrefix}ApiResources__{i}__ApiSecret", apiSecret);

            int si = 0;
            foreach (var scope in scopes ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}ApiResources__{i}__Scopes__{si++}__Name", scope.ToLower());

            builder.MigApiResourceIndex++;
        });

        return builder;
    }

    #endregion

    #region Roles

    public static IdentityServerNetMigrationBuilder WithUserRole(
            this IdentityServerNetMigrationBuilder builder,
            string role)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
            e.EnvironmentVariables.Add(
                $"{MigEnvPrefix}Roles__{builder.MigApiUserRoleIndex++}__Name",
                role.ToLower()));

        return builder;
    }

    public static IdentityServerNetMigrationBuilder AddUserRoles(
            this IdentityServerNetMigrationBuilder builder,
            IEnumerable<string>? userRoles)
    {
        foreach (var r in userRoles ?? [])
            builder.WithUserRole(r);

        return builder;
    }

    #endregion

    #region Users

    public static IdentityServerNetMigrationBuilder WithUser(
            this IdentityServerNetMigrationBuilder builder,
            string username,
            string password,
            IEnumerable<string>? roles = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            int i = builder.MigApiUserIndex;
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{i}__Name", username.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{i}__Password", password);

            int ri = 0;
            foreach (var role in roles ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Users__{i}__Roles__{ri++}", role);

            builder.MigApiUserIndex++;
        });

        return builder;
    }

    #endregion

    #region Clients

    /// <summary>
    /// Adds a client using a plain URL string.
    /// </summary>
    public static IdentityServerNetMigrationBuilder AddClient(
            this IdentityServerNetMigrationBuilder builder,
            ClientType clientType,
            string clientId,
            string clientSecret,
            string? clientUrl = null,
            IEnumerable<string>? scopes = null,
            IEnumerable<string>? additionalRedirectUris = null,
            IEnumerable<string>? additionalGrantTypes = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            int i = builder.MigClientIndex;
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientType", clientType.ToString());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientId", clientId.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientSecret", clientSecret);

            if (!string.IsNullOrEmpty(clientUrl))
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientUrl", clientUrl);

            int si = 0;
            foreach (var scope in scopes ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__Scopes__{si++}", scope.ToLower());

            int ri = 0;
            foreach (var uri in additionalRedirectUris ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__AdditionalRedirectUris__{ri++}", uri);

            int gi = 0;
            foreach (var grant in additionalGrantTypes ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__AdditionalGrantTypes__{gi++}", grant);

            builder.MigClientIndex++;
        });

        return builder;
    }

    /// <summary>
    /// Adds a client and derives the URL from an Aspire resource's HTTPS endpoint.
    /// </summary>
    public static IdentityServerNetMigrationBuilder AddClient(
            this IdentityServerNetMigrationBuilder builder,
            ClientType clientType,
            string clientId,
            string clientSecret,
            IResourceWithEndpoints resource,
            IEnumerable<string>? scopes = null,
            IEnumerable<string>? additionalRedirectUris = null,
            IEnumerable<string>? additionalGrantTypes = null)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            int i = builder.MigClientIndex;
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientType", clientType.ToString());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientId", clientId.ToLower());
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientSecret", clientSecret);
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__ClientUrl",
                resource.GetEndpoint("https")?.Url ?? "");

            int si = 0;
            foreach (var scope in scopes ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__Scopes__{si++}", scope.ToLower());

            int ri = 0;
            foreach (var uri in additionalRedirectUris ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__AdditionalRedirectUris__{ri++}", uri);

            int gi = 0;
            foreach (var grant in additionalGrantTypes ?? [])
                e.EnvironmentVariables.Add($"{MigEnvPrefix}Clients__{i}__AdditionalGrantTypes__{gi++}", grant);

            builder.MigClientIndex++;
        });

        return builder;
    }

    // ── Convenience overloads ────────────────────────────────────────────────

    /// <summary>Adds a WebApplication client (Authorization Code + PKCE).</summary>
    public static IdentityServerNetMigrationBuilder AddWebApplicationClient(
            this IdentityServerNetMigrationBuilder builder,
            string clientId,
            string clientSecret,
            string clientUrl,
            IEnumerable<string>? scopes = null,
            IEnumerable<string>? additionalRedirectUris = null,
            IEnumerable<string>? additionalGrantTypes = null)
        => builder.AddClient(ClientType.WebApplication, clientId, clientSecret, clientUrl,
                scopes, additionalRedirectUris, additionalGrantTypes);

    /// <inheritdoc cref="AddWebApplicationClient(IdentityServerNetMigrationBuilder,string,string,string,IEnumerable{string}?,IEnumerable{string}?,IEnumerable{string}?)"/>
    public static IdentityServerNetMigrationBuilder AddWebApplicationClient(
            this IdentityServerNetMigrationBuilder builder,
            string clientId,
            string clientSecret,
            IResourceWithEndpoints resource,
            IEnumerable<string>? scopes = null,
            IEnumerable<string>? additionalRedirectUris = null,
            IEnumerable<string>? additionalGrantTypes = null)
        => builder.AddClient(ClientType.WebApplication, clientId, clientSecret, resource,
                scopes, additionalRedirectUris, additionalGrantTypes);

    /// <summary>Adds a machine-to-machine API client (Client Credentials).</summary>
    public static IdentityServerNetMigrationBuilder AddApiClient(
            this IdentityServerNetMigrationBuilder builder,
            string clientId,
            string clientSecret,
            IEnumerable<string>? scopes = null)
        => builder.AddClient(ClientType.ApiClient, clientId, clientSecret,
                clientUrl: null, scopes);

    /// <summary>Adds a JavaScript / SPA client (Authorization Code + PKCE, no secret).</summary>
    public static IdentityServerNetMigrationBuilder AddJavaScriptClient(
            this IdentityServerNetMigrationBuilder builder,
            string clientId,
            string clientUrl,
            IEnumerable<string>? scopes = null,
            IEnumerable<string>? additionalRedirectUris = null)
        => builder.AddClient(ClientType.JavascriptClient, clientId, clientSecret: "",
                clientUrl, scopes, additionalRedirectUris);

    #endregion
}
