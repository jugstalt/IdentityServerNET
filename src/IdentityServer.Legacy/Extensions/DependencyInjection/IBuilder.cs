using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    public interface IBuilder
    {
        IServiceCollection Services { get; }
    }
}
