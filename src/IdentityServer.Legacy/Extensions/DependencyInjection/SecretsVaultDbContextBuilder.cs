using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    class SecretsVaultDbContextBuilder : Builder, ISecretsVaultDbContextBuilder
    {
        public SecretsVaultDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
