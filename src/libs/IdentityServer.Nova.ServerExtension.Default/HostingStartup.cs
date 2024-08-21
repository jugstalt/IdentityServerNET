using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.EmailSender;
using IdentityServer.Nova.Extensions;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.LiteDb.Services.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Reflection;
using IdentityServer.Nova.ServerExtension.Default.Extensions;
using IdentityServer.Nova.ServerExtension.Default.Services.DbContext;
using IdentityServer.Nova.ServerExtension.Default.Services.UI;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.Services.EmailSender;
using IdentityServer.Nova.Services.PasswordHasher;
using IdentityServer.Nova.Services.Security;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;

namespace IdentityServer.Nova.ServerExtension.Default;

[IdentityServerNovaStartup]
public class TestHostingStartup : IIdentityServerNovaStartup
{
    public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
    {
        #region Add a PasswordHasher (optional)

        services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>();

        #endregion

        #region Add an UserDbContext (required)

        services.AddTransient<IUserStoreFactory, DefaultUserStoreFactory>();

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:FilesDb"]))
        {
            services.AddUserDbContext<FileBlobUserDb>(options =>
            {
                options.ConnectionString = Path.Combine(context.Configuration["ConnectionStrings:FilesDb"], "users");
                options.AddDefaults(context.Configuration);
            });
        }
        else if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:LiteDb"]))
        {
            services.AddUserDbContext<LiteDbUserDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:LiteDb"];
                options.AddDefaults(context.Configuration);
            });
        }
        else
        {
            services.AddUserDbContext<InMemoryUserDb>(options =>
                    options.AddDefaults(context.Configuration)
                );
        }

        #endregion

        #region Add RoleDbContext (optional) 

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:FilesDb"]))
        {
            services.AddRoleDbContext<FileBlobRoleDb>(options =>
            {
                options.ConnectionString = Path.Combine(context.Configuration["ConnectionStrings:FilesDb"], "roles");
            });
        }
        else if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:LiteDb"]))
        {
            services.AddRoleDbContext<LiteDbRoleDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:LiteDb"];
            });
        }
        else
        {
            services.AddRoleDbContext<InMemoryRoleDb>();
        }

        #endregion

        #region Add a ResourceDbContext (required) 

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:FilesDb"]))
        {
            services.AddResourceDbContext<FileBlobResourceDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "resources");
                options.AddDefaults(context.Configuration);
            });
        }
        else if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:LiteDb"]))
        {
            services.AddResourceDbContext<LiteDbResourceDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:LiteDb"];
                options.AddDefaults(context.Configuration);
            });
        }
        else
        {
            services.AddResourceDbContext<InMemoryResourceDb>(options =>
                    options.AddDefaults(context.Configuration)
                );
        }


        /*
        services.AddResourceDbContext<MongoBlobResourceDb>(options =>
        {
            options.ConnectionString = context.Configuration["ConnectionStrings:MongoDb"]; //"mongodb://localhost:27017";
        });
        */
        /*
        services.AddResourceDbContext<TableStorageBlobResourceDb>(options =>
        {
            options.ConnectionString = context.Configuration["ConnectionStrings:AzureStorage"];
        });
        */

        #endregion

        #region Add a ClientDbContext (required)

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:FilesDb"]))
        {
            services.AddClientDbContext<FileBlobClientDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "clients");
                options.AddDefaults(context.Configuration);
            });
        }
        else if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:LiteDb"]))
        {
            services.AddClientDbContext<LiteDbClientDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:LiteDb"];
                options.AddDefaults(context.Configuration);
            });
        }
        else
        {
            services.AddClientDbContext<InMemoryClientDb>(options =>
                    options.AddDefaults(context.Configuration)
                );
        }

        /*
        services.AddClientDbContext<MongoBlobClientDb>(options =>
        {
            options.ConnectionString = context.Configuration["ConnectionStrings:MongoDb"]; //"mongodb://localhost:27017";
        });
        */
        /*
        services.AddClientDbContext<TableStorageBlobClientDb>(options =>
        {
            options.ConnectionString = context.Configuration["ConnectionStrings:AzureStorage"];
        });
        */
        #endregion

        #region Add ExportClientDbContext (optional)

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:ExportPath"]))
        {
            services.AddExportClientDbContext<FileBlobClientExportDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:ExportPath"];
            });
        }

        #endregion

        #region Add ExportResourceDbContext (optional)

        if (!String.IsNullOrEmpty(context.Configuration["ConnectionStrings:ExportPath"]))
        {
            services.AddExportResourceDbContext<FileBlobResourceExportDb>(options =>
            {
                options.ConnectionString = context.Configuration["ConnectionStrings:ExportPath"];
            });
        }

        #endregion

        #region App SecretsVaultDbContext (optional) 

        services.AddSecretsVaultDbContext<FileBlobSecretsVaultDb>(context.Configuration, options =>
        {
            options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "secretsvault");
            options.CryptoService = new Base64CryptoService();
        });

        #endregion

        #region UI (required)

        services.AddUserInterfaceService<DefaultUserInterfaceService>(options =>
        {
            options.ApplicationTitle = context.Configuration["ApplicationTitle"];  // required
            options.OverrideCssContent = Properties.Resources.is4_overrides + Properties.Resources.openid_logo;
            options.MediaContent = new Dictionary<string, byte[]>()
            {
                { "openid-logo.png", Properties.Resources.openid_logo }
            };
        });

        #endregion

        #region EmailSender (required)

        services.AddTransient<ICustomEmailSender, MailJetEmailSender>();

        #endregion

        #region Login BotDetection (optional)

        services.AddLoginBotDetection<LoginBotDetection>();
        services.AddCaptchaRenderer<CaptchaCodeRenderer>();

        #endregion
    }
}
