using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    class RoleDbContextBuilder : Builder, IRoleDbContextBuilder
    {
        public RoleDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
