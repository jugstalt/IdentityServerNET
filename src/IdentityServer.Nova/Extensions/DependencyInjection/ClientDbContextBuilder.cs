using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    class ClientDbContextBuilder : Builder, IClientDbContextBuilder
    {
        public ClientDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
