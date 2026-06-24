using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

namespace IdentityServerNET.Aspire.Hosting.Utilitities;

public static class MailPitResourceBuilderExtensions
{
    public static IResourceBuilder<MailPitResource> AddMailPit(
        this IDistributedApplicationBuilder builder,
        string name,
        int? httpPort = null,
        int? smtpPort = null)
    {
        var resource = new MailPitResource(name);

        return builder.AddResource(resource)
                      .WithImage(MailPitContainerImageTags.Image)
                      .WithImageRegistry(MailPitContainerImageTags.Registry)
                      .WithImageTag(MailPitContainerImageTags.Tag)
                      .WithContainerRuntimeArgs("--name", resource.ContainerName)
                      .WithHttpEndpoint(
                          targetPort: resource.ContainerHttpPort,
                          port: httpPort,
                          name: MailPitResource.HttpEndpointName)
                      .WithEndpoint(
                          targetPort: resource.ContainerSmtpPort,
                          port: smtpPort,
                          name: MailPitResource.SmtpEndpointName);
    }
}

internal static class MailPitContainerImageTags
{
    internal const string Registry = "docker.io";
    internal const string Image = "axllent/mailpit";
    internal const string Tag = "latest";
}
