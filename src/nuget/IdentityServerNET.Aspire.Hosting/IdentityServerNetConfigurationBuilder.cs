namespace Aspire.Hosting.ApplicationModel;

public class IdentityServerNetConfigurationBuilder(IResourceBuilder<IdentityServerNetResource> resourceBuilder)
{
    internal IResourceBuilder<IdentityServerNetResource> ResourceBuilder { get; } = resourceBuilder;
}