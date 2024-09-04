using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.EventSinks;
using IdentityServer.Nova.Abstractions.Security;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Abstractions.UI;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.UI;
using IdentityServer.Nova.Servivces.DbContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;

namespace IdentityServer.Nova.Extensions.DependencyInjection;

public static class ServicesExtensions
{
    public static IServiceCollection IfServiceNotRegistered<TService>(this IServiceCollection services, Action action)
    {
        if (services.Any(s => s.ServiceType == typeof(TService)) == false)
        {
            action();
        }

        return services;
    }

    #region UserDbContext

    static public IServiceCollection AddUserDbContext<T>(this IServiceCollection services, Action<UserDbContextConfiguration> setupAction)
        where T : class, IUserDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IUserDbContext, T>();

        return services;
    }


    static public IServiceCollection AddUserDbContext<T, TOptions>(this IServiceCollection services, Action<TOptions> setupAction)
        where T : class, IUserDbContext
        where TOptions : UserDbContextConfiguration
    {
        services.Configure<TOptions>(setupAction);
        services.AddTransient<IUserDbContext, T>();

        return services;
    }

    #endregion

    #region ClientDbContext

    static public IServiceCollection AddClientDbContext<T>(this IServiceCollection services, Action<ClientDbContextConfiguration> setupAction)
        where T : class, IClientDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IClientDbContext, T>();

        return services;
    }

    #endregion

    #region ExportClientDbContext

    static public IServiceCollection AddExportClientDbContext<T>(this IServiceCollection services, Action<ExportClientDbContextConfiguration> setupAction)
        where T : class, IExportClientDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IExportClientDbContext, T>();

        return services;
    }

    #endregion

    #region ResourceDbContext

    static public IServiceCollection AddResourceDbContext<T>(this IServiceCollection services, Action<ResourceDbContextConfiguration> setupAction)
        where T : class, IResourceDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IResourceDbContext, T>();

        return services;
    }

    #endregion

    #region ResourceDbContext

    static public IServiceCollection AddExportResourceDbContext<T>(this IServiceCollection services, Action<ExportResourceDbContextConfiguration> setupAction)
        where T : class, IExportResourceDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IExportResourceDbContext, T>();

        return services;
    }

    #endregion

    #region RoleDbContext

    static public IRoleDbContextBuilder AddRoleDbContext<T>(this IServiceCollection services)
        where T : class, IRoleDbContext
    {
        services.AddTransient<IRoleDbContext, T>();

        return new RoleDbContextBuilder(services);
    }

    static public IServiceCollection AddRoleDbContext<T>(this IServiceCollection services, Action<RoleDbContextConfiguration> setupAction)
        where T : class, IRoleDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<IRoleDbContext, T>();

        return services;
    }

    #endregion

    #region SecretsVaultDb

    static public IServiceCollection AddSecretsVaultDbContext<T>(this IServiceCollection services, IConfiguration configuration, Action<SecretsVaultDbContextConfiguration> setupAction)
        where T : class, ISecretsVaultDbContext
    {
        services.Configure(setupAction);
        services.AddTransient<ISecretsVaultDbContext, T>();

        if (!String.IsNullOrEmpty(configuration["BlobCryptoKey"]))
        {
            services.AddTransient<IVaultSecretCryptoService, DefaultCryptoService>();
        }
        else
        {
            services.AddTransient<IVaultSecretCryptoService, Base64CryptoService>();
        }

        return services;
    }

    #endregion

    #region Bot Detection

    public static IServiceCollection AddLoginBotDetection<TBotDetectionType>(this IServiceCollection services)
       where TBotDetectionType : class, ILoginBotDetection
    {
        services.AddTransient<ILoginBotDetection, TBotDetectionType>();

        return services;
    }

    public static IServiceCollection AddLoginBotDetection<TBotDetectionType, TBotDetectionOptionsType>(this IServiceCollection services, Action<TBotDetectionOptionsType> botOptionsSetup)
        where TBotDetectionType : class, ILoginBotDetection
        where TBotDetectionOptionsType : class
    {
        services.Configure<TBotDetectionOptionsType>(botOptionsSetup);

        return services.AddLoginBotDetection<TBotDetectionType>();
    }

    public static IServiceCollection AddCaptchaRenderer<TCaptchaRendererType>(this IServiceCollection services)
       where TCaptchaRendererType : class, ICaptchaCodeRenderer
    {
        services.AddTransient<ICaptchaCodeRenderer, TCaptchaRendererType>();

        return services;
    }

    public static IServiceCollection AddCaptchaRenderer<TCaptchaRendererType, TCaptchaRendererOptionsType>(this IServiceCollection services, Action<TCaptchaRendererOptionsType> captchaOptionsSetup)
        where TCaptchaRendererType : class, ICaptchaCodeRenderer
        where TCaptchaRendererOptionsType : class
    {
        services.Configure<TCaptchaRendererOptionsType>(captchaOptionsSetup);

        return services.AddCaptchaRenderer<TCaptchaRendererType>();
    }

    #endregion

    #region IdentityEventSink

    public static IServiceCollection AddIdentityEventSink<TSinkType>(this IServiceCollection services)
        where TSinkType : class, IIdentityEventSink
    {
        services.AddSingleton<IIdentityEventSink, TSinkType>();

        return services;
    }

    public static IServiceCollection AddIdentityEventSink<TSinkType, TSinkOpitonsType>(this IServiceCollection services, Action<TSinkOpitonsType> optionsSetup)
        where TSinkType : class, IIdentityEventSink
        where TSinkOpitonsType : class
    {
        services.Configure<TSinkOpitonsType>(optionsSetup);
        return services.AddIdentityEventSink<TSinkType>();
    }

    #endregion

    #region UserInterface 

    static public IServiceCollection AddUserInterfaceService<T>(this IServiceCollection services, Action<UserInterfaceServiceOptions> configureService)
        where T : class, IUserInterfaceService
    {
        services.Configure<UserInterfaceServiceOptions>(configureService);
        services.AddTransient<IUserInterfaceService, T>();

        return services;
    }

    #endregion

    #region CryptoService

    public static IServiceCollection AddCryptoServices(this IServiceCollection services, IConfiguration configuration)
    {
        Type implementationType = services.CryptoServiceType(configuration, true);

        services.AddTransient(typeof(ICryptoService), implementationType);
        services.AddTransient(typeof(IVaultSecretCryptoService), implementationType);

        return services;
    }

    private static Type CryptoServiceType(this IServiceCollection services, IConfiguration configuration, bool addConfiguration = false)
    {
        Func<Type> GetCryptoService = configuration["IdentityServer:Crypto:Method"]?.ToLower() switch
        {
            "key" => () =>
            {
                if (addConfiguration)
                {
                    services.Configure<DefaultCryptoServiceOptions>(config =>
                    {
                        config.Password = configuration["IdentityServer:Crypto:Key"];
                    });
                }
                return typeof(DefaultCryptoService);
            },
            "data-protection" => () => typeof(DataProtectionCryptoService),
            _ => () => typeof(Base64CryptoService)
        };

        return GetCryptoService();
    }

    #endregion
}
