using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.EmailSender;
using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Abstractions.SigningCredential;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Azure.Services.DbContext;
using IdentityServerNET.Distribution.Extensions.DependencyInjection;
using IdentityServerNET.Factories;
using IdentityServerNET.HttpProxy.Services.DbContext;
using IdentityServerNET.LiteDb.Services.DbContext;
using IdentityServerNET.Models;
using IdentityServerNET.MongoDb.Services.DbContext;
using IdentityServerNET.Reflection;
using IdentityServerNET.ServerExtension.Default.Extensions;
using IdentityServerNET.Services;
using IdentityServerNET.Services.Cryptography;
using IdentityServerNET.Services.DbContext;
using IdentityServerNET.Services.EmailSender;
using IdentityServerNET.Services.PasswordHasher;
using IdentityServerNET.Services.Security;
using IdentityServerNET.Services.SigningCredential;
using IdentityServerNET.Services.UI;
using IdentityServerNET.Stores;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace IdentityServerNET.Extensions.DependencyInjection;

static public class ServiceCollectionExtensions
{
    static public IServiceCollection AddColorSchemeService(this IServiceCollection services)
        => services.AddScoped<ColorSchemeService>();

    static public IServiceCollection AddUserStore(this IServiceCollection services)
        => services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
    static public IServiceCollection AddRoleStore(this IServiceCollection services)
        => services.AddTransient<IRoleStore<ApplicationRole>, RoleStoreProxy>();

    static public IServiceCollection AddSigningCredentialCertificateStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddTransient<ICertificateFactory, CertificateFactory>();

        if (String.IsNullOrEmpty(configuration.ValidationCertsPath()))
        {
            // not recommended for production - you need to store your key material somewhere secure
            services.AddSingleton<ISigningCredentialCertificateStorage, SigningCredentialCertificateInMemoryStorage>();
        }
        else
        {
            services.Configure<SigningCredentialCertificateStorageOptions>(storageOptions =>
            {
                storageOptions.Storage = configuration.ValidationCertsPath();
                storageOptions.CertPassword = configuration["IdentityServer:SigningCredential:CertPassword"] ?? "Secu4epas3wOrd";
            });
            services.AddTransient<ISigningCredentialCertificateStorage, SigningCredentialCertificateFileSystemStorage>();
        }

        return services;
    }

    static public IServiceCollection AddServicesFromConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection("IdentityServer");

        // Default PasswordHasher (override aspnet core defaults)
        // for custom Hashers override this with ConfigureCustomStartup
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
            options.ConnectionString = configuration.SecretsVaultPath();
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
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "users");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Users:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddUserDbContext<LiteDbUserDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Users:HttpProxy", "ConnectionStrings:HttpProxy"], value =>
                        services
                        .AddUserDbContext<HttpProxyUserDb>(options =>
                        {
                            options.AddDefaults(configSection);
                        })
                        .AddHttpInvoker<IAdminUserDbContext>(invoker =>
                        {
                            invoker.UrlPath = "api/users";
                        },
                        client =>
                        {
                            client.BaseAddress = new Uri(value);
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
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "roles");
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Roles:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddRoleDbContext<LiteDbRoleDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Roles:HttpProxy", "ConnectionStrings:HttpProxy"], value =>
                        services
                        .AddRoleDbContext<HttpProxyRoleDb>(_ => { })
                        .AddHttpInvoker<IAdminRoleDbContext>(invoker =>
                        {
                            invoker.UrlPath = "api/roles";
                        },
                        client =>
                        {
                            client.BaseAddress = new Uri(value);
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
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "resources");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddResourceDbContext<LiteDbResourceDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
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
                    .SwitchCase(["ConnectionStrings:Resources:HttpProxy", "ConnectionStrings:HttpProxy"], value =>
                        services
                        .AddResourceDbContext<HttpProxyResourceDb>(_ => { })
                        .AddHttpInvoker<IResourceDbContextModify>(invoker =>
                        {
                            invoker.UrlPath = "api/resources";
                        },
                        client =>
                        {
                            client.BaseAddress = new Uri(value);
                        })
                    )
                    .SwitchDefault(() =>
                        services.AddResourceDbContext<InMemoryResourceDb>(options =>
                            options.AddDefaults(configSection)
                        )
                    )
            )

            // Default ClientDbContext
            .IfServiceNotRegistered<IClientDbContext>(() =>
                configSection
                    .SwitchCase(["ConnectionStrings:Clients:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddClientDbContext<FileBlobClientDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "clients");
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddClientDbContext<LiteDbClientDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
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
                    .SwitchCase(["ConnectionStrings:Clients:HttpProxy", "ConnectionStrings:HttpProxy"], value =>
                        services
                        .AddClientDbContext<HttpProxyClientDb>(_ => { })
                        .AddHttpInvoker<IClientDbContextModify>(invoker =>
                        {
                            invoker.UrlPath = "api/clients";
                        },
                        client =>
                        {
                            client.BaseAddress = new Uri(value);
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
                options.ApplicationTitle = configSection["ApplicationTitle"] ?? "IdentityServer NET";
                options.OverrideCssContent = IdentityServer.Properties.Resources.is4_overrides;
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

    static public IServiceCollection ConfigureCustomStartup(this IServiceCollection services, IConfiguration configuration)
    {
        //services.AddTransient<IPasswordHasher<ApplicationUser>, ClearPasswordHasher>();

        string isAssemblyName = configuration["IdentityServer:AssemblyName"] ?? "IdentityServerNET.ServerExtension.Default";
        if (!String.IsNullOrWhiteSpace(isAssemblyName))
        {
            var assembly = Assembly.LoadFrom($"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}/{isAssemblyName}.dll");

            foreach (var type in assembly.GetTypes())
            {
                if (type.GetCustomAttribute<IdentityServerStartupAttribute>() != null)
                {
                    if (type.GetInterfaces().Any(i => i.Equals(typeof(IIdentityServerStartup))))
                    {
                        var hostingStartup = Activator.CreateInstance(type) as IIdentityServerStartup;
                        hostingStartup.ConfigureServices(services, configuration);
                    }
                }
            }
        }

        return services;
    }
}
