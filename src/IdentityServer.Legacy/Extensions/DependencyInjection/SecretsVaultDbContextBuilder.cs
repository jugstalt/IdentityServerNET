using Microsoft.Extensions.DependencyInjection;

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
