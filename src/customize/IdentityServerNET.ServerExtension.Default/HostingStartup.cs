using IdentityServerNET.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.ServerExtension.Default;

[IdentityServerStartup]
public class TestHostingStartup : IIdentityServerStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // Add your custom services here
    }
}
