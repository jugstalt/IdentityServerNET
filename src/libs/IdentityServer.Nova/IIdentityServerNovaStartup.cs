using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova;

public interface IIdentityServerNovaStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
