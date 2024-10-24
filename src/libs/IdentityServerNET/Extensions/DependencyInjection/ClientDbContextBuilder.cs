using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class ClientDbContextBuilder : Builder, IClientDbContextBuilder
{
    public ClientDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
