using IdentityServer.Legacy.Services.DbContext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.DependencyInjection
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
    }
}
