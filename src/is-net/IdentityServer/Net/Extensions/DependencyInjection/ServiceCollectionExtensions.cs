using IdentityServer4.Stores;
using IdentityServer4.Stores.Default;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.EmailSender;
using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.SigningCredential;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Azure.Services.DbContext;
using IdentityServerNET.CaptchaRenderers;
using IdentityServerNET.Distribution.Extensions.DependencyInjection;
using IdentityServerNET.Factories;
using IdentityServerNET.HttpProxy.Services.DbContext;
using IdentityServerNET.LiteDb.Services.DbContext;
using IdentityServerNET.Models;
using IdentityServerNET.MongoDb.Services.DbContext;
using IdentityServerNET.Postgres.Services.DbContext;
using IdentityServerNET.Reflection;
using IdentityServerNET.Services;
using IdentityServerNET.Services.Cryptography;
using IdentityServerNET.Services.DbContext;
using IdentityServerNET.Services.EmailSender;
using IdentityServerNET.Services.PasswordHasher;
using IdentityServerNET.Services.Security;
using IdentityServerNET.Services.SigningCredential;
using IdentityServerNET.Services.UI;
using IdentityServerNET.Sqlite.Services.DbContext;
using IdentityServerNET.SqlServer.Services.DbContext;
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
    {
        services.AddTransient<IUserStore<ApplicationUser>, UserStoreProxy>();
        services.AddTransient<IUserPasskeyStore<ApplicationUser>, UserStoreProxy>();
        return services;
    }
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
        services.AddOptions<PasswordHashingOptions>()
            .Configure(options =>
            {
                var template = configuration["IdentityServer:Security:PasswordHashing:Template"];
                if (!string.IsNullOrWhiteSpace(template))
                    options.Template = template;
            })
            .Validate(
                options => options.Template.Contains("{password}"),
                "IdentityServer:Security:PasswordHashing:Template must contain the {password} placeholder — " +
                "hashing without the actual password is not allowed.")
            .ValidateOnStart();
        services.AddTransient<IPasswordHasher<ApplicationUser>, SecurePasswordHasher>();

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
                    .SwitchCase(["ConnectionStrings:Users:SqlServer", "ConnectionStrings:SqlServer"], value =>
                        services.AddUserDbContext<SqlServerUserDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Users:Postgres", "ConnectionStrings:Postgres"], value =>
                        services.AddUserDbContext<PostgresUserDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Users:Sqlite", "ConnectionStrings:Sqlite"], value =>
                        services.AddUserDbContext<SqliteUserDb>(options =>
                        {
                            options.ConnectionString = value;
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
                    .SwitchCase(["ConnectionStrings:Roles:SqlServer", "ConnectionStrings:SqlServer"], value =>
                        services.AddRoleDbContext<SqlServerRoleDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Roles:Postgres", "ConnectionStrings:Postgres"], value =>
                        services.AddRoleDbContext<PostgresRoleDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Roles:Sqlite", "ConnectionStrings:Sqlite"], value =>
                        services.AddRoleDbContext<SqliteRoleDb>(options =>
                        {
                            options.ConnectionString = value;
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
                    .SwitchCase(["ConnectionStrings:Resources:SqlServer", "ConnectionStrings:SqlServer"], value =>
                        services.AddResourceDbContext<SqlServerResourceDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:Postgres", "ConnectionStrings:Postgres"], value =>
                        services.AddResourceDbContext<PostgresResourceDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Resources:Sqlite", "ConnectionStrings:Sqlite"], value =>
                        services.AddResourceDbContext<SqliteResourceDb>(options =>
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
                    .SwitchCase(["ConnectionStrings:Clients:SqlServer", "ConnectionStrings:SqlServer"], value =>
                        services.AddClientDbContext<SqlServerClientDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:Postgres", "ConnectionStrings:Postgres"], value =>
                        services.AddClientDbContext<PostgresClientDb>(options =>
                        {
                            options.ConnectionString = value;
                            options.AddDefaults(configSection);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Clients:Sqlite", "ConnectionStrings:Sqlite"], value =>
                        services.AddClientDbContext<SqliteClientDb>(options =>
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

            // Default RealmDbContext (multi-tenancy). Always registered; harmless until realms are used.
            .IfServiceNotRegistered<IRealmDbContext>(() =>
                configSection
                    .SwitchCase(["ConnectionStrings:Realms:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddRealmDbContext<FileBlobRealmDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "realms");
                        })
                    )
                    .SwitchCase(["ConnectionStrings:Realms:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddRealmDbContext<LiteDbRealmDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
                        })
                    )
                    .SwitchDefault(() =>
                        services.AddRealmDbContext<InMemoryRealmDb>(_ => { })
                    )
            )

            // Realm context — resolves the current realm from the logged-in user's e-mail domain.
            // Consumed by the realm-scoping DbContext decorators.
            .IfServiceNotRegistered<IRealmContext>(() =>
            {
                services.AddHttpContextAccessor();
                services.AddScoped<IRealmContext, RealmContext>();
            })

            // Default UIDbContext
            .IfServiceNotRegistered<IUIDbContext>(() =>
                configSection
                    .SwitchCase(["ConnectionStrings:UI:FilesDb", "ConnectionStrings:FilesDb"], value =>
                        services.AddUIDbContext<FileBlobUIDb>(options =>
                        {
                            options.ConnectionString = Path.Combine(configuration.StorageAssetPath(value), "ui");
                        })
                    )
                    .SwitchCase(["ConnectionStrings:UI:LiteDb", "ConnectionStrings:LiteDb"], value =>
                        services.AddUIDbContext<LiteDbUIDb>(options =>
                        {
                            options.ConnectionString = configuration.AssetPath(value);
                        })
                    )
                    .SwitchCase(["ConnectionStrings:UI:SqlServer", "ConnectionStrings:SqlServer"], value =>
                        services.AddUIDbContext<SqlServerUIDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchCase(["ConnectionStrings:UI:Postgres", "ConnectionStrings:Postgres"], value =>
                        services.AddUIDbContext<PostgresUIDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchCase(["ConnectionStrings:UI:Sqlite", "ConnectionStrings:Sqlite"], value =>
                        services.AddUIDbContext<SqliteUIDb>(options =>
                        {
                            options.ConnectionString = value;
                        })
                    )
                    .SwitchDefault(() =>
                        services.AddTransient<IUIDbContext, InMemoryUIDb>()
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

            // Mail template service
            .IfServiceNotRegistered<IMailTemplateService>(() =>
                services.AddSingleton<IMailTemplateService, MailTemplateService>()
            )

            // Default UserInterface
            .IfServiceNotRegistered<IUserInterfaceService>(() => services.AddUserInterfaceService<DefaultUserInterfaceService>(options =>
            {
                options.ApplicationTitle = configSection["ApplicationTitle"] ?? "IdentityServer NET";
                options.OverrideCssContent = IdentityServer.Properties.Resources.is4_overrides;
            }))
            // BotDetection
            .IfServiceNotRegistered<ILoginBotDetection>(() => services.AddLoginBotDetection<LoginBotDetection>())
            // Captcha
            .IfServiceNotRegistered<ICaptchaCodeRenderer>(() => services.AddCaptchaRenderer<ModernCaptchaCodeRenderer>())
            // PushedAuthorizationRequestStore (optional – distributed store for PAR request_uri; default: InMemory)
            // Config: IdentityServer:Stores:PushedAuthorizationStore = DistributedMemoryCache | DistributedRedisCache
            .IfServiceNotRegistered<IPushedAuthorizationRequestStore>(() =>
                configSection
                    .SwitchCase(["Stores:PushedAuthorizationStore"], value =>
                    {
                        if (string.Equals(value, "DistributedMemoryCache", StringComparison.OrdinalIgnoreCase))
                        {
                            services.AddDistributedMemoryCache();
                            services.AddTransient<IPushedAuthorizationRequestStore, DistributedCachePushedAuthorizationRequestStore>();
                        }
                        else if (string.Equals(value, "DistributedRedisCache", StringComparison.OrdinalIgnoreCase))
                        {
                            var connectionString = configSection["Stores:PushedAuthorizationStoreConnectionString"]
                                ?? throw new InvalidOperationException(
                                    "IdentityServer:Stores:PushedAuthorizationStoreConnectionString is required for DistributedRedisCache");
                            services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
                            services.AddTransient<IPushedAuthorizationRequestStore, DistributedCachePushedAuthorizationRequestStore>();
                        }
                    })
                    .SwitchDefault(() => { })  // no config → InMemory (registered by IS4 Core.cs via TryAddSingleton)
            )

            // AuthorizationParametersMessageStore (optional – keeps authorize params server-side instead of in the ReturnUrl)
            // Config: IdentityServer:Stores:ParameterMessageStore = DistributedMemoryCache | DistributedRedisCache
            .IfServiceNotRegistered<IAuthorizationParametersMessageStore>(() =>
                configSection
                    .SwitchCase(["Stores:ParameterMessageStore"], value =>
                    {
                        if (string.Equals(value, "DistributedMemoryCache", StringComparison.OrdinalIgnoreCase))
                        {
                            services.AddDistributedMemoryCache();
                            services.AddTransient<IAuthorizationParametersMessageStore, DistributedCacheAuthorizationParametersMessageStore>();
                        }
                        else if (string.Equals(value, "DistributedRedisCache", StringComparison.OrdinalIgnoreCase))
                        {
                            var connectionString = configSection["Stores:ParameterMessageStoreConnectionString"]
                                ?? throw new InvalidOperationException(
                                    "IdentityServer:Stores:ParameterMessageStoreConnectionString is required for DistributedRedisCache");
                            services.AddStackExchangeRedisCache(options => options.Configuration = connectionString);
                            services.AddTransient<IAuthorizationParametersMessageStore, DistributedCacheAuthorizationParametersMessageStore>();
                        }
                    })
                    .SwitchDefault(() => { })  // no config → no registration (IdentityServer default: params in ReturnUrl)
            );

        services.AddSingleton<UICustomizationService>();

        return services;
    }

    static public IServiceCollection ConfigureCustomStartup(
            this IServiceCollection services,
            IConfiguration configuration,
            IIdentityServerBuilder identityServerBuilder)
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
                        hostingStartup.ConfigureServices(services, configuration, identityServerBuilder);
                    }
                }
            }
        }

        return services;
    }
}
