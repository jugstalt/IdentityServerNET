using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

class ResourceDbContextBuilder : Builder, IResourceDbContextBuilder
{
    public ResourceDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
