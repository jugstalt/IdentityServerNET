using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class UserDbContextBuilder : Builder, IUserDbContextBuilder
    {
        public UserDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
