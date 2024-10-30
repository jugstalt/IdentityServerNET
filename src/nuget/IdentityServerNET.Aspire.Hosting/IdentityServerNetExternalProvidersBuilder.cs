namespace Aspire.Hosting.ApplicationModel;

public class IdentityServerNetExternalProvidersBuilder(IResourceBuilder<IdentityServerNetResource> resourceBuilder)
{
    internal IResourceBuilder<IdentityServerNetResource> ResourceBuilder { get; } = resourceBuilder;
}
