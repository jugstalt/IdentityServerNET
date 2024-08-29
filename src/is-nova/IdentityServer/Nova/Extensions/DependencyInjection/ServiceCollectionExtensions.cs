using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.EmailSender;
using IdentityServer.Nova.Abstractions.Security;
using IdentityServer.Nova.Abstractions.UI;
using IdentityServer.Nova.Azure.Services.DbContext;
using IdentityServer.Nova.LiteDb.Services.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Reflection;
using IdentityServer.Nova.ServerExtension.Default.Extensions;
using IdentityServer.Nova.Services;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.Services.EmailSender;
using IdentityServer.Nova.Services.PasswordHasher;
using IdentityServer.Nova.Services.Security;
using IdentityServer.Nova.Services.UI;
using IdentityServer.Nova.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddColorSchemeService(this IServiceCollection services)
        => services.AddScoped<ColorSchemeService>();

    static public IServiceCollection AddUserStore(this IServiceCollection services)
        => services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
    static public IServiceCollection AddRoleStore(this IServiceCollection services)
        => services.AddTransient<IRoleStore<ApplicationRole>, RoleStoreProxy>();

    static public IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection("IdentityServer");

        #region Add an UserDbContext (required)

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Users:FilesDb"]))
        {
            services.AddUserDbContext<FileBlobUserDb>(options =>
            {
                options.ConnectionString = Path.Combine(configSection["ConnectionStrings:Users:FilesDb"], "users");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Users:LiteDb"]))
        {
            services.AddUserDbContext<LiteDbUserDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Users:LiteDb"];
                options.AddDefaults(configSection);
            });
        }

        #endregion

        #region Add RoleDbContext (optional) 

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Roles:FilesDb"]))
        {
            services.AddRoleDbContext<FileBlobRoleDb>(options =>
            {
                options.ConnectionString = Path.Combine(configSection["ConnectionStrings:Roles:FilesDb"], "roles");
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Roles:LiteDb"]))
        {
            services.AddRoleDbContext<LiteDbRoleDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Roles:LiteDb"];
            });
        }

        #endregion

        #region Add a ResourceDbContext (required) 

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Resources:FilesDb"]))
        {
            services.AddResourceDbContext<FileBlobResourceDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "resources");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Resources:LiteDb"]))
        {
            services.AddResourceDbContext<LiteDbResourceDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Resources:LiteDb"];
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Resources:AzureStorage"]))
        {
            services.AddResourceDbContext<TableStorageBlobResourceDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Resources:AzureStorage"];
                options.AddDefaults(configSection);
            });
        }

        #endregion

        #region Add a ClientDbContext (required)

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Clients:FilesDb"]))
        {
            services.AddClientDbContext<FileBlobClientDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "clients");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Clients:LiteDb"]))
        {
            services.AddClientDbContext<LiteDbClientDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Clients:LiteDb"];
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:Clients:AzureStorage"]))
        {
            services.AddClientDbContext<TableStorageBlobClientDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:Clients:AzureStorage"];
                options.AddDefaults(configSection);
            });
        }

        #endregion

        #region Add ExportClientDbContext (optional)

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:ExportPath"]))
        {
            services.AddExportClientDbContext<FileBlobClientExportDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:ExportPath"];
            });
        }

        #endregion

        #region Add ExportResourceDbContext (optional)

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:ExportPath"]))
        {
            services.AddExportResourceDbContext<FileBlobResourceExportDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:ExportPath"];
            });
        }

        #endregion

        #region App SecretsVaultDbContext (optional) 

        services.AddSecretsVaultDbContext<FileBlobSecretsVaultDb>(configSection, options =>
        {
            options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "secretsvault");
            options.CryptoService = new Base64CryptoService();
        });

        #endregion

        #region EmailSender (required)

        if (configSection.GetSection("Mail:Smtp").Exists())
        {
            services.AddTransient<ICustomEmailSender, SmtpEmailSender>();
        }
        else if (configSection.GetSection("Mail:MailJet").Exists())
        {
            services.AddTransient<ICustomEmailSender, MailJetEmailSender>();
        }
        else if (configSection.GetSection("Mail:SendGrid").Exists())
        {
            services.AddTransient<ICustomEmailSender, SendGridEmailSender>();
        }

        #endregion

        return services;
    }

    static public IServiceCollection AddFallbackServices(this IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection("IdentityServer");

        services
            // Default PasswordHasher
            .IfServiceNotRegistered<IPasswordHasher<ApplicationUser>>(() => services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>())
            // Default UserStoreFactory
            .IfServiceNotRegistered<IUserStoreFactory>(() => services.AddTransient<IUserStoreFactory, DefaultUserStoreFactory>())
            // Default UserDbContext
            .IfServiceNotRegistered<IUserDbContext>(() => services.AddUserDbContext<InMemoryUserDb>(options =>
                    options.AddDefaults(configSection)
                ))
            // Default RoleDbContex
            .IfServiceNotRegistered<IRoleDbContext>(() => services.AddRoleDbContext<InMemoryRoleDb>())
            // Default ResouceDbContext
            .IfServiceNotRegistered<IResourceDbContext>(() => services.AddResourceDbContext<InMemoryResourceDb>(options =>
                    options.AddDefaults(configSection)
                ))
            // Default ClientDbContext
            .IfServiceNotRegistered<IClientDbContext>(() => services.AddClientDbContext<InMemoryClientDb>(options =>
                    options.AddDefaults(configSection)
                ))
            // Default EmailSender
            .IfServiceNotRegistered<ICustomEmailSender>(() => services.AddTransient<ICustomEmailSender, NullEmailSender>())
            // Default UserInterface
            .IfServiceNotRegistered<IUserInterfaceService>(() => services.AddUserInterfaceService<DefaultUserInterfaceService>(options =>
            {
                options.ApplicationTitle = configSection["ApplicationTitle"] ?? "IdentityServer.Nova";
                options.OverrideCssContent = Properties.Resources.is4_overrides;
                //options.MediaContent = new Dictionary<string, byte[]>()
                //{
                //    { "openid-logo.png", Properties.Resources.openid_logo }
                //};
            }))
            // BotDetection
            .IfServiceNotRegistered<ILoginBotDetection>(() => services.AddLoginBotDetection<LoginBotDetection>())
            // Captcha
            .IfServiceNotRegistered<ICaptchCodeRenderer>(() => services.AddCaptchaRenderer<CaptchaCodeRenderer>());

        return services;
    }

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
