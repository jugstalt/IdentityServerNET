// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the .NET Aspire hosting package automatically adds this namespace.
using Aspire.Hosting.ApplicationModel;
using IdentityServerNET.Aspire.Hosting.Utilitities;
using System;

namespace Aspire.Hosting;

static public class IdentityServerNetResourceBuilderExtensions
{
    public static IdentityServerNetResourceBuilder AddIdentityServerNET(
        this IDistributedApplicationBuilder builder,
        string containerName,
        int? httpPort = null,
        int? httpsPort = null,
        string? imageTag = null,
        string? bridgeNetwork = null)
    {
        var resource = new IdentityServerNetResource(containerName);

        var resourceBuilder = builder.AddResource(resource)
                      .WithImage(IdentityServerNetContainerImageTags.Image)
                      .WithImageRegistry(IdentityServerNetContainerImageTags.Registry)
                      .WithImageTag(imageTag ?? IdentityServerNetContainerImageTags.Tag)
                      .WithHttpEndpoint(
                          targetPort: 8080,
                          port: httpPort,
                          name: IdentityServerNetResource.HttpEndpointName)
                      .WithHttpsEndpoint(
                          targetPort: 8443,
                          port: httpsPort,
                          name: IdentityServerNetResource.HttpsEndpointName);

        if (!String.IsNullOrEmpty(bridgeNetwork))
        {
            resourceBuilder.WithContainerRuntimeArgs("--network", bridgeNetwork);
        }

        return new IdentityServerNetResourceBuilder(
            builder,
            resourceBuilder);
    }

    public static IdentityServerNetResourceBuilder WithBindMountPersistance(
        this IdentityServerNetResourceBuilder builder,
        string persistancePath = "{{local-app-data}}/identityserver-net-aspire")
    {
        builder.ResourceBuilder.WithBindMount(
                persistancePath.Replace("{{local-app-data}}", Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)),
                "/home/app/identityserver-net",
                isReadOnly: false
             );

        return builder;
    }

    public static IdentityServerNetResourceBuilder WithVolumePersistance(
        this IdentityServerNetResourceBuilder builder,
        string volumneName = "identityserver-net")
    {
        builder.ResourceBuilder.WithBindMount(
                volumneName,
                "/home/app/identityserver-net",
                isReadOnly: false
             );

        return builder;
    }

    public static IdentityServerNetResourceBuilder WithMailDev(
        this IdentityServerNetResourceBuilder builder,
        int? smtpPort = null
        )
    {
        var mailDev = builder.AppBuilder.AddMailDev(
            name: $"{builder.ResourceBuilder.Resource.Name}-maildev",
            smtpPort: smtpPort);

        builder.ResourceBuilder
            .WithEnvironment(e =>
            {
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__FromEmail", "no-reply@is.net");
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__FromName", "IdentityServer NET");

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

    public static IdentityServerNetResourceBuilder WithMigrations(
        this IdentityServerNetResourceBuilder builder,
        Action<IdentityServerNetMigrationBuilder> migrationBuilder)
    {
        migrationBuilder(new IdentityServerNetMigrationBuilder(builder.ResourceBuilder));

        return builder;
    }

    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IResourceBuilder<IdentityServerNetResource> isNet,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.WithEnvironment(e =>
        {
            e.EnvironmentVariables.Add(
                configName.Replace(":", "__"),
                isNet.Resource.HttpsEndpoint);
        });

    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IdentityServerNetResourceBuilder isNet,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.AddReference(isNet.ResourceBuilder, configName);

    public static IResourceBuilder<IdentityServerNetResource> Build(
        this IdentityServerNetResourceBuilder builder) => builder.ResourceBuilder;
}

public class IdentityServerNetResourceBuilder(
        IDistributedApplicationBuilder appBuilder,
        IResourceBuilder<IdentityServerNetResource> resourceBuilder
    )
{
    internal IDistributedApplicationBuilder AppBuilder { get; } = appBuilder;
    internal IResourceBuilder<IdentityServerNetResource> ResourceBuilder { get; } = resourceBuilder;
}

public enum ClientType
{
    Empty,
    WebApplication,
    ApiClient,
    JavascriptClient
}

internal static class IdentityServerNetContainerImageTags
{
    internal const string Registry = "docker.io";
    internal const string Image = "gstalt/identityserver-net-dev";
    internal const string Tag = "latest";
}

