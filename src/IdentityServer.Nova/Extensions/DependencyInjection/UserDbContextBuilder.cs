using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    class UserDbContextBuilder : Builder, IUserDbContextBuilder
    {
        public UserDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
