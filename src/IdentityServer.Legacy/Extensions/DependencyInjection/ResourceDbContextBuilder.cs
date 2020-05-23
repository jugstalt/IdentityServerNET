using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class ResourceDbContextBuilder : Builder, IResourceDbContextBuilder
    {
        public ResourceDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
