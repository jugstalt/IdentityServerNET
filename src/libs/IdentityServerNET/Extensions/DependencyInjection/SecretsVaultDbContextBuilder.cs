using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class SecretsVaultDbContextBuilder : Builder, ISecretsVaultDbContextBuilder
{
    public SecretsVaultDbContextBuilder(IServiceCollection services)
        : base(services)
    {

    }
}
