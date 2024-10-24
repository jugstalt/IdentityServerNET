using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class UserDbContextBuilder : Builder, IUserDbContextBuilder
{
    public UserDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
