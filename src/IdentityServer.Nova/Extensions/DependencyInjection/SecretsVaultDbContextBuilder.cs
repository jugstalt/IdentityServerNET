using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    class SecretsVaultDbContextBuilder : Builder, ISecretsVaultDbContextBuilder
    {
        public SecretsVaultDbContextBuilder(IServiceCollection services)
            : base(services)
        {

        }
    }
}
