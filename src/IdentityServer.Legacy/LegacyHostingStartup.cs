using System;
using System.Linq;
using System.Reflection;
using IdentityServer.Legacy.Reflection;
using IdentityServer.Legacy.Stores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

[assembly: HostingStartup(typeof(IdentityServer.Legacy.LegacyHostingStartup))]
namespace IdentityServer.Legacy
{
    public class LegacyHostingStartup : IHostingStartup
    {
        public LegacyHostingStartup()
        {
        }

        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
                //services.AddTransient<IPasswordHasher<ApplicationUser>, ClearPasswordHasher>();

                // ToDo:
                // ServerExtension.Test wird nur aufgerufen, wenn sie auch im Projekt IdentityServer als Dependency geführt wird!
                // Hier sollte es einen Mechanischmus geben, dass man das Startuphosting auch für die "eingbunden dll laden kann"
                // Der Name der Assembly sollte in der Config stehen -> GetTypes -> IHostingStartup -> Invoke

                string legacyAssemblyName = context.Configuration["LegacyAssemblyName"];
                if(!String.IsNullOrWhiteSpace(legacyAssemblyName))
                {
                    var legacyAssembly = Assembly.LoadFrom($"{ System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) }/{ legacyAssemblyName }.dll");

                    foreach(var type in legacyAssembly.GetTypes())
                    {
                        if (type.GetCustomAttribute<IdentityServerLegacyStartupAttribute>() != null)
                        {
                            if (type.GetInterfaces().Any(i => i.Equals(typeof(IIdentityServerLegacyStartup))))
                            {
                                var hostingStartup = Activator.CreateInstance(type) as IIdentityServerLegacyStartup;
                                hostingStartup.ConfigureServices(context, services);
                            }
                        }
                    }
                }
            });
        }
    }
}
