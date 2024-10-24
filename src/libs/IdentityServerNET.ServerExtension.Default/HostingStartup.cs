using IdentityServerNET.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityServerNET.ServerExtension.Default;

[IdentityServerStartup]
public class TestHostingStartup : IIdentityServerStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        // nothing to do here, every defaults win dumm in 
        //   services.AddDatabases  &
        //   services.AddFallbackServices

        //var configSection = configuration.GetSection("IdentityServer");

        //#region UI (required)

        //services.AddUserInterfaceService<DefaultUserInterfaceService>(options =>
        //{
        //    options.ApplicationTitle = configSection["ApplicationTitle"] ?? "IdentityServer NET";
        //    options.OverrideCssContent = Properties.Resources.is4_overrides + Properties.Resources.openid_logo;
        //    options.MediaContent = new Dictionary<string, byte[]>()
        //    {
        //        { "openid-logo.png", Properties.Resources.openid_logo }
        //    };
        //});

        //#endregion
    }
}
