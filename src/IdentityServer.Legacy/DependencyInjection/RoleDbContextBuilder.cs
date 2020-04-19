using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.DependencyInjection
{
    class RoleDbContextBuilder : Builder, IRoleDbContextBuilder
    {
        public RoleDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
