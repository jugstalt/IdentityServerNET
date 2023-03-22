using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class ClientDbContextBuilder : Builder, IClientDbContextBuilder
    {
        public ClientDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
