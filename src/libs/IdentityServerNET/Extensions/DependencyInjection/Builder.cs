using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

class Builder : IBuilder
{
    public Builder(IServiceCollection services)
    {
        this.Services = services;
    }

    public IServiceCollection Services { get; private set; }
}
