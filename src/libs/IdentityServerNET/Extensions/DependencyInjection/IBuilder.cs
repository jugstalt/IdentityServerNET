using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.Extensions.DependencyInjection;

public interface IBuilder
{
    IServiceCollection Services { get; }
}
