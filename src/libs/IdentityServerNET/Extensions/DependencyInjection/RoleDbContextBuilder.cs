using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class RoleDbContextBuilder : Builder, IRoleDbContextBuilder
{
    public RoleDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
