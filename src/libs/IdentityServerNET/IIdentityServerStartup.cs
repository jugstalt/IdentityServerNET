using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET;

public interface IIdentityServerStartup
{
    void ConfigureServices(IServiceCollection services, IConfiguration configuration);
}
