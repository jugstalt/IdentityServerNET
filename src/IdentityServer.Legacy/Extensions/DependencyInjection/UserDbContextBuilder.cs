using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

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
