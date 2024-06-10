using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection
{
    public interface IBuilder
    {
        IServiceCollection Services { get; }
    }
}
