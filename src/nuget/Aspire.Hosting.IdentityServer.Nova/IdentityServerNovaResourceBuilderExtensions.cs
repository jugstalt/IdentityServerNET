// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the .NET Aspire hosting package automatically adds this namespace.
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.IdentityServerNova.Utilitities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Aspire.Hosting;

static public class IdentityServerNovaResourceBuilderExtensions
{
    public static IdentityServerNovaResourceBuilder AddIdentityServerNova(
        this IDistributedApplicationBuilder builder,
        string containerName,
        int? httpPort = null,
        int? httpsPort = null,
        string? imageTag = null,
        string? bridgeNetwork = null)
    {
        var resource = new IdentityServerNovaResource(containerName);

        var resourceBuilder = builder.AddResource(resource)
                      .WithImage(IdentityServerNovaContainerImageTags.Image)
                      .WithImageRegistry(IdentityServerNovaContainerImageTags.Registry)
                      .WithImageTag(imageTag ?? IdentityServerNovaContainerImageTags.Tag)
                      .WithHttpEndpoint(
                          targetPort: 8080,
                          port: httpPort,
                          name: IdentityServerNovaResource.HttpEndpointName)
                      .WithHttpsEndpoint(
                          targetPort: 8443,
                          port: httpsPort,
                          name: IdentityServerNovaResource.HttpsEndpointName);

        if(!String.IsNullOrEmpty(bridgeNetwork))
        {
            resourceBuilder.WithContainerRuntimeArgs("--network", bridgeNetwork);
        }

        return new IdentityServerNovaResourceBuilder(
            builder,
            resourceBuilder);
    }

    public static IdentityServerNovaResourceBuilder WithBindMountPersistance(
        this IdentityServerNovaResourceBuilder builder,
        string persistancePath = "{{local-app-data}}/identityserver-nova-aspire")
    {
        builder.ResourceBuilder.WithBindMount(
                persistancePath.Replace("{{local-app-data}}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                "/home/app/identityserver-nova",
                isReadOnly: false
             );

        return builder;
    }

    public static IdentityServerNovaResourceBuilder WithVolumePersistance(
        this IdentityServerNovaResourceBuilder builder,
        string volumneName = "identityserver-nova")
    {
        builder.ResourceBuilder.WithBindMount(
                volumneName,
                "/home/app/identityserver-nova",
                isReadOnly: false
             );

        return builder;
    }

    public static IdentityServerNovaResourceBuilder WithMailDev(
        this IdentityServerNovaResourceBuilder builder,
        int? smtpPort = null
        )
    {
        var mailDev = builder.AppBuilder.AddMailDev(
            name: $"{builder.ResourceBuilder.Resource.Name}-maildev",
            smtpPort: smtpPort);

        builder.ResourceBuilder
            .WithEnvironment(e =>
            {
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__FromEmail", "no-reply@is-nova.com");
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__FromName", "IdentityServer Nova");

                e.EnvironmentVariables.Add(
                    "IdentityServer__Mail__Smtp__SmtpServer",
                    //mailDev.Resource.SmtpEndpoint.ContainerHost
                    mailDev.Resource.ContainerName);

                e.EnvironmentVariables.Add(
                    "IdentityServer__Mail__Smtp__SmtpPort",
                    //mailDev.Resource.SmtpEndpoint.Port //.Property(EndpointProperty.Port)
                    mailDev.Resource.ContainerSmtpPort.ToString());

                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__EnableSsl", false.ToString());
            });

        builder.ResourceBuilder
            .WaitFor(mailDev);

        return builder;
    }

    public static IResourceBuilder<IdentityServerNovaResource> AsResourceBuilder(this IdentityServerNovaResourceBuilder builder)
        => builder.ResourceBuilder;

    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IResourceBuilder<IdentityServerNovaResource> nova,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add(
                configName.Replace(":", "__"),
                nova.Resource.HttpsEndpoint);
        });

    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IdentityServerNovaResourceBuilder nova,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.AddReference(nova.ResourceBuilder, configName);

    #region Migrations

    private const string MigEnvPrefix = "IdentityServer__Migrations__";

    public static IdentityServerNovaResourceBuilder WithAdminPassword(
            this IdentityServerNovaResourceBuilder builder,
            string adminPassword) 
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}AdminPassword", adminPassword);
        });

        return builder;
    }

    #region IdentityResources

    public static IdentityServerNovaResourceBuilder WithIdentityResouce(
            this IdentityServerNovaResourceBuilder builder,
            string identityResouce)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}IdentityResources__{builder.MigIdentityResourceIndex++}__Name", identityResouce.ToLower());
        });

        return builder;
    }

    public static IdentityServerNovaResourceBuilder WithIdentityResources(
            this IdentityServerNovaResourceBuilder builder,
            IEnumerable<string> identityResources
        )
    {
        identityResources?.ToList().ForEach(r => builder.WithIdentityResouce(r)); 
        
        return builder;
    }

    #endregion

    #region ApiResources

    public static IdentityServerNovaResourceBuilder WithApiResource(
            this IdentityServerNovaResourceBuilder builder,
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

    public static IdentityServerNovaResourceBuilder WithUserRole(
            this IdentityServerNovaResourceBuilder builder,
            string role)
    {
        builder.ResourceBuilder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add($"{MigEnvPrefix}Roles__{builder.MigApiUserRoleIndex++}__Name", role.ToLower());
        });

        return builder;
    }

    public static IdentityServerNovaResourceBuilder WithUserRoles(
            this IdentityServerNovaResourceBuilder builder,
            IEnumerable<string>? userRoles)
    {
        userRoles?.ToList().ForEach(r => builder.WithUserRole(r));
        
        return builder;
    }

    #endregion

    #region Users

    public static IdentityServerNovaResourceBuilder WithUser(
            this IdentityServerNovaResourceBuilder builder,
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

    public static IdentityServerNovaResourceBuilder WithClient(
            this IdentityServerNovaResourceBuilder builder,
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

            if(!String.IsNullOrEmpty(clientUrl))
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
    public static IdentityServerNovaResourceBuilder WithClient(
            this IdentityServerNovaResourceBuilder builder,
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

    #endregion
}

public class IdentityServerNovaResourceBuilder(
        IDistributedApplicationBuilder appBuilder,
        IResourceBuilder<IdentityServerNovaResource> resourceBuilder
    )
{
    internal IDistributedApplicationBuilder AppBuilder { get; } = appBuilder;
    internal IResourceBuilder<IdentityServerNovaResource> ResourceBuilder { get; } = resourceBuilder;

    internal int MigIdentityResourceIndex = 0;
    internal int MigApiResourceIndex = 0;
    internal int MigApiUserRoleIndex = 0;
    internal int MigApiUserIndex = 0;
    internal int MigClientIndex = 0;
}

public enum ClientType
{
    Empty,
    WebApplication,
    ApiClient,
    JavascriptClient
}

internal static class IdentityServerNovaContainerImageTags
{
    internal const string Registry = "docker.io";
    internal const string Image = "gstalt/identityserver-nova-dev";
    internal const string Tag = "latest";
}

