using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class RoleDbContextBuilder : Builder, IRoleDbContextBuilder
    {
        public RoleDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
