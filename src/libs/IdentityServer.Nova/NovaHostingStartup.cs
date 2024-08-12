using IdentityServer.Nova.Models;
using IdentityServer.Nova.Reflection;
using IdentityServer.Nova.Stores;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

[assembly: HostingStartup(typeof(IdentityServer.Nova.NovaHostingStartup))]
namespace IdentityServer.Nova;

public class NovaHostingStartup : IHostingStartup
{
    public NovaHostingStartup()
    {
    }

    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices((context, services) =>
        {
            services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
            services.AddTransient<IRoleStore<ApplicationRole>, RoleStoreProxy>();

            //services.AddTransient<IPasswordHasher<ApplicationUser>, ClearPasswordHasher>();

            string novaAssemblyName = context.Configuration["NovaAssemblyName"] ?? "IdentityServer.Nova.ServerExtension.Default";
            if (!String.IsNullOrWhiteSpace(novaAssemblyName))
            {
                var novaAssembly = Assembly.LoadFrom($"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/{novaAssemblyName}.dll");

                foreach (var type in novaAssembly.GetTypes())
                {
                    if (type.GetCustomAttribute<IdentityServerNovaStartupAttribute>() != null)
                    {
                        if (type.GetInterfaces().Any(i => i.Equals(typeof(IIdentityServerNovaStartup))))
                        {
                            var hostingStartup = Activator.CreateInstance(type) as IIdentityServerNovaStartup;
                            hostingStartup.ConfigureServices(context, services);
                        }
                    }
                }
            }
        });
    }
}
