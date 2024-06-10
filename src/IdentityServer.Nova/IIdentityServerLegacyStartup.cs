using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova
{
    public interface IIdentityServerLegacyStartup
    {
        void ConfigureServices(WebHostBuilderContext context, IServiceCollection services);
    }
}
