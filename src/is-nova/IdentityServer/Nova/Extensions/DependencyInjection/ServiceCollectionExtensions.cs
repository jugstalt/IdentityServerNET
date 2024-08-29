using IdentityServer.Nova.Models;
using IdentityServer.Nova.Reflection;
using IdentityServer.Nova.Services;
using IdentityServer.Nova.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System;
using System.Linq;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddColorSchemeService(this IServiceCollection services)
        => services.AddScoped<ColorSchemeService>();

    static public IServiceCollection AddUserStore(this IServiceCollection services)
        => services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
    static public IServiceCollection AddRoleStore(this IServiceCollection services)
        =>services.AddTransient<IRoleStore<ApplicationRole>, RoleStoreProxy>();

    static public IServiceCollection ConfigureCustomNovaStartup(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddTransient<IPasswordHasher<ApplicationUser>, ClearPasswordHasher>();

        string novaAssemblyName = configuration["IdentityServer:NovaAssemblyName"] ?? "IdentityServer.Nova.ServerExtension.Default";
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
                        hostingStartup.ConfigureServices(services, configuration);
                    }
                }
            }
        }

        return services;
    }
 }
