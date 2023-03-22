using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Legacy
{
    public interface IIdentityServerLegacyStartup
    {
        void ConfigureServices(WebHostBuilderContext context, IServiceCollection services);
    }
}
