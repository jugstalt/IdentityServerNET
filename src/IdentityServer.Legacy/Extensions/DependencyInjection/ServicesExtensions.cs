using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Extensions.DependencyInjection
{
    public static class ServicesExtensions
    {
        #region UserDbContext

        static public IUserDbContextBuilder AddUserDbContext<T>(this IServiceCollection services) 
            where T : class, IUserDbContext
        {
            services.AddTransient<IUserDbContext, T>();

            return new UserDbContextBuilder(services);
        }

        static public IServiceCollection AddUserDbContext<T>(this IServiceCollection services, Action<UserDbContextConfiguration> setupAction)
            where T : class, IUserDbContext
        {
            services.Configure(setupAction);
            services.AddTransient<IUserDbContext, T>();

            return services;
        }

        public static IUserDbContextBuilder AddUserDbContextOptions(this IUserDbContextBuilder builder, 
            Action<UserDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
        }

        #endregion

        #region ClientDbContext

        static public IClientDbContextBuilder AddClientDbContext<T>(this IServiceCollection services)
            where T : class, IClientDbContext
        {
            services.AddTransient<IClientDbContext, T>();

            return new ClientDbContextBuilder(services);
        }

        static public IServiceCollection AddClientDbContext<T>(this IServiceCollection services, Action<ClientDbContextConfiguration> setupAction)
            where T : class, IClientDbContext
        {
            services.Configure(setupAction);
            services.AddTransient<IClientDbContext, T>();

            return services;
        }

        public static IClientDbContextBuilder AddClientDbContextOptions(this IClientDbContextBuilder builder,
            Action<ClientDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
        }

        #endregion

        #region ExportClientDbContext

        static public IClientDbContextBuilder AddExportClientDbContext<T>(this IServiceCollection services)
            where T : class, IExportClientDbContext
        {
            services.AddTransient<IExportClientDbContext, T>();

            return new ClientDbContextBuilder(services);
        }

        static public IServiceCollection AddExportClientDbContext<T>(this IServiceCollection services, Action<ExportClientDbContextConfiguration> setupAction)
            where T : class, IExportClientDbContext
        {
            services.Configure(setupAction);
            services.AddTransient<IExportClientDbContext, T>();

            return services;
        }

        public static IClientDbContextBuilder AddExportClientDbContextOptions(this IClientDbContextBuilder builder,
            Action<ExportClientDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
        }

        #endregion

        #region ResourceDbContext

        static public IResourceDbContextBuilder AddResourceDbContext<T>(this IServiceCollection services)
            where T : class, IResourceDbContext
        {
            services.AddTransient<IResourceDbContext, T>();

            return new ResourceDbContextBuilder(services);
        }

        static public IServiceCollection AddResourceDbContext<T>(this IServiceCollection services, Action<ResourceDbContextConfiguration> setupAction)
            where T : class, IResourceDbContext
        {
            services.Configure(setupAction);
            services.AddTransient<IResourceDbContext, T>();

            return services;
        }

        public static IResourceDbContextBuilder AddResourceDbContextOptions(this IResourceDbContextBuilder builder,
            Action<ResourceDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
        }

        #endregion

        #region ResourceDbContext

        static public IResourceDbContextBuilder AddExportResourceDbContext<T>(this IServiceCollection services)
            where T : class, IExportResourceDbContext
        {
            services.AddTransient<IExportResourceDbContext, T>();

            return new ResourceDbContextBuilder(services);
        }

        static public IServiceCollection AddExportResourceDbContext<T>(this IServiceCollection services, Action<ExportResourceDbContextConfiguration> setupAction)
            where T : class, IExportResourceDbContext
        {
            services.Configure(setupAction);
            services.AddTransient<IExportResourceDbContext, T>();

            return services;
        }

        public static IResourceDbContextBuilder AddExportResourceDbContextOptions(this IResourceDbContextBuilder builder,
            Action<ExportResourceDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
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

        public static IRoleDbContextBuilder AddRoleDbContextOptions(this IRoleDbContextBuilder builder,
            Action<RoleDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
        }

        #endregion

        #region SecretsVaultDb

        static public ISecretsVaultDbContextBuilder AddSecretsVaultDbContext<T, TSecretEncryptor>(this IServiceCollection services)
            where T : class, ISecretsVaultDbContext
            where TSecretEncryptor : class, IVaultSecretCryptoService
        {
            services.AddTransient<ISecretsVaultDbContext, T>();
            services.AddTransient<IVaultSecretCryptoService, TSecretEncryptor>();

            return new SecretsVaultDbContextBuilder(services);
        }

        static public IServiceCollection AddSecretsVaultDbContext<T, TSecretEncryptor>(this IServiceCollection services, Action<SecretsVaultDbContextConfiguration> setupAction)
            where T : class, ISecretsVaultDbContext
            where TSecretEncryptor : class, IVaultSecretCryptoService
        {
            services.Configure(setupAction);
            services.AddTransient<ISecretsVaultDbContext, T>();
            services.AddTransient<IVaultSecretCryptoService, TSecretEncryptor>();

            return services;
        }

        public static ISecretsVaultDbContextBuilder AddSecretsVaultDbContextOptions(this ISecretsVaultDbContextBuilder builder,
            Action<SecretsVaultDbContextConfiguration> setupAction)
        {
            builder.Services.Configure(setupAction);

            return builder;
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
           where TCaptchaRendererType : class, ICaptchCodeRenderer
        {
            services.AddTransient<ICaptchCodeRenderer, TCaptchaRendererType>();

            return services;
        }

        public static IServiceCollection AddCaptchaRenderer<TCaptchaRendererType, TCaptchaRendererOptionsType>(this IServiceCollection services, Action<TCaptchaRendererOptionsType> captchaOptionsSetup)
            where TCaptchaRendererType : class, ICaptchCodeRenderer
            where TCaptchaRendererOptionsType : class
        {
            services.Configure<TCaptchaRendererOptionsType>(captchaOptionsSetup);

            return services.AddCaptchaRenderer<TCaptchaRendererType>();
        }

        #endregion
    }
}
