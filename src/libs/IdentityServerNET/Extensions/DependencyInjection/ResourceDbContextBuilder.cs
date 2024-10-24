using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class ResourceDbContextBuilder : Builder, IResourceDbContextBuilder
{
    public ResourceDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
