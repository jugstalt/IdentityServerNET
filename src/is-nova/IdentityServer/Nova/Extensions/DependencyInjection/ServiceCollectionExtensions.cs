using IdentityServer.Nova.Services;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddColorSchemeService(this IServiceCollection services)
        => services.AddScoped<ColorSchemeService>();
}
