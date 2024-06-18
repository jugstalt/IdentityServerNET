using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova
{
    public interface IIdentityServerNovaStartup
    {
        void ConfigureServices(WebHostBuilderContext context, IServiceCollection services);
    }
}
