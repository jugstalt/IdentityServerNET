// Put extensions in the Aspire.Hosting namespace to ease discovery as referencing
// the .NET Aspire hosting package automatically adds this namespace.
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.IdentityServerNova.Utilitities;
using System;

namespace Aspire.Hosting;

static public class IdentityServerNovaResourceBuilderExtensions
{
    public static IdentityServerNovaResourceBuilder AddIdentityServerNova(
        this IDistributedApplicationBuilder builder,
        string containerName,
        int? httpPort = null,
        int? httpsPort = null,
        string? imageTag = null)
    {
        var resource = new IdentityServerNovaResource(containerName);

        return new IdentityServerNovaResourceBuilder(
            builder,
            builder.AddResource(resource)
                      .WithImage(IdentityServerNovAContainerImageTags.Image)
                      .WithImageRegistry(IdentityServerNovAContainerImageTags.Registry)
                      .WithImageTag(imageTag ?? IdentityServerNovAContainerImageTags.Tag)
                      .WithHttpEndpoint(
                          targetPort: 8080,
                          port: httpPort,
                          name: IdentityServerNovaResource.HttpEndpointName)
                      .WithHttpsEndpoint(
                          targetPort: 8443,
                          port: httpsPort,
                          name: IdentityServerNovaResource.HttpsEndpointName));
    }

    public static IdentityServerNovaResourceBuilder WithBindMountPersistance(
        this IdentityServerNovaResourceBuilder builder,
        string persistancePath = "{{user-profile}}/identityserver-nova-aspire")
    {
        builder.ResourceBuilder.WithBindMount(
                persistancePath.Replace("{{user-profile}}", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)),
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
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__SmtpServer", mailDev.Resource.SmtpEndpoint.ContainerHost);
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__SmtpPort", mailDev.Resource.SmtpEndpoint.Property(EndpointProperty.Port));
                e.EnvironmentVariables.Add("IdentityServer__Mail__Smtp__EnableSsl", false.ToString());
            });

        builder.ResourceBuilder.WithReference(mailDev);

        return builder;
    }

    public static IResourceBuilder<IdentityServerNovaResource> AsResourceBuilder(this IdentityServerNovaResourceBuilder builder)
        => builder.ResourceBuilder;

    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IResourceBuilder<IdentityServerNovaResource> nova,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.WithEnvironment(
            configName.Replace(":", "__"),
            nova.Resource.HttpsEndpoint);


    public static IResourceBuilder<T> AddReference<T>(
            this IResourceBuilder<T> builder,
            IdentityServerNovaResourceBuilder nova,
            string configName
        ) where T : IResourceWithEnvironment
        => builder.AddReference(nova.ResourceBuilder, configName);
}

public class IdentityServerNovaResourceBuilder(
        IDistributedApplicationBuilder appBuilder,
        IResourceBuilder<IdentityServerNovaResource> resourceBuilder
    )
{
    internal IDistributedApplicationBuilder AppBuilder { get; } = appBuilder;
    internal IResourceBuilder<IdentityServerNovaResource> ResourceBuilder { get; } = resourceBuilder;
}

internal static class IdentityServerNovAContainerImageTags
{
    internal const string Registry = "docker.io";
    internal const string Image = "gstalt/identityserver-nova-dev";
    internal const string Tag = "5.24.4201";
}

