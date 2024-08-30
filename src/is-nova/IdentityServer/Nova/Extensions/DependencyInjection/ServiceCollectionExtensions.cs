using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.EmailSender;
using IdentityServer.Nova.Abstractions.Security;
using IdentityServer.Nova.Abstractions.UI;
using IdentityServer.Nova.Azure.Services.DbContext;
using IdentityServer.Nova.LiteDb.Services.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.MongoDb.Services.DbContext;
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
using IdentityServer4.Models;
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

        // Default PasswordHasher (override aspnet core defaults)
        // for custom Hashers override this with ConfigureCustomNovaStartup
        services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>();

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

        return services;
    }

    static public IServiceCollection AddFallbackServices(this IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection("IdentityServer");

        services
            // Default UserStoreFactory
            .IfServiceNotRegistered<IUserStoreFactory>(() => services.AddTransient<IUserStoreFactory, DefaultUserStoreFactory>())
            
            // Default UserDbContext
            .IfServiceNotRegistered<IUserDbContext>(() =>
                configSection
                    .SwitchCase(["ConnectionStrings:Users:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddUserDbContext<FileBlobUserDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(value, "users");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Users:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddUserDbContext<LiteDbUserDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchDefault(() =>
                        services.AddUserDbContext<InMemoryUserDb>(options =>
                            options.AddDefaults(configSection)
                        )
                    )
            )

            // Default RoleDbContex
            .IfServiceNotRegistered<IRoleDbContext>(() => 
                configSection
                    .SwitchCase(["ConnectionStrings:Roles:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddRoleDbContext<FileBlobRoleDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(value, "roles");
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Roles:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddRoleDbContext<LiteDbRoleDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchDefault(() => 
                        services.AddRoleDbContext<InMemoryRoleDb>()
                    )
            )

            // Default ResouceDbContext
            .IfServiceNotRegistered<IResourceDbContext>(() => 
                configSection
                    .SwitchCase(["ConnectionStrings:Resources:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddResourceDbContext<FileBlobResourceDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(value, "resources");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddResourceDbContext<LiteDbResourceDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:AzureStorage", "ConnectionStrings:AzureStorage"], value =>
                        services.AddResourceDbContext<TableStorageBlobResourceDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:MongoDb", "ConnectionStrings:MongoDb"], value =>
                        services.AddResourceDbContext<MongoBlobResourceDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchDefault(()=>
                        services.AddResourceDbContext<InMemoryResourceDb>(options =>
                            options.AddDefaults(configSection)
                        )
                    )
            )

            // Default ClientDbContext
            .IfServiceNotRegistered<IClientDbContext>(() =>
                configSection
                    .SwitchCase(["ConnectionStrings:Clients:FilesDb","ConnectionStrings:FilesDb"], value =>
                        services.AddClientDbContext<FileBlobClientDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(value, "clients");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddClientDbContext<LiteDbClientDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:AzureStorage", "ConnectionStrings:AzureStorage"], value =>
                        services.AddClientDbContext<TableStorageBlobClientDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:MongoDb", "ConnectionStrings:MongoDb"], value =>
                        services.AddClientDbContext<MongoBlobClientDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchDefault(() =>
                        services.AddClientDbContext<InMemoryClientDb>(options =>
                                options.AddDefaults(configSection)
                        )
                    )
            )

            // Default EmailSender
            .IfServiceNotRegistered<ICustomEmailSender>(() => 
                configSection
                    .SwitchSection("Mail:Smtp", _ => services.AddTransient<ICustomEmailSender, SmtpEmailSender>())
                    .SwitchSection("Mail:MailJet", _ => services.AddTransient<ICustomEmailSender, MailJetEmailSender>())
                    .SwitchSection("Mail:SendGrid", _ => services.AddTransient<ICustomEmailSender, SendGridEmailSender>())
                    .SwitchDefault(() => services.AddTransient<ICustomEmailSender, NullEmailSender>())
            )
            
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
            .IfServiceNotRegistered<ICaptchaCodeRenderer>(() => services.AddCaptchaRenderer<CaptchaCodeRenderer>());

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
