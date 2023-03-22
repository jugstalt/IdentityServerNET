using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class ResourceDbContextBuilder : Builder, IResourceDbContextBuilder
    {
        public ResourceDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
