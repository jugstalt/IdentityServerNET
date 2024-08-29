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
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;

namespace IdentityServer.Nova.ServerExtension.Default;

[IdentityServerNovaStartup]
public class TestHostingStartup : IIdentityServerNovaStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        var configSection = configuration.GetSection("IdentityServer");

        #region Add a PasswordHasher (optional)

        services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>();

        #endregion

        #region Add an UserDbContext (required)

        services.AddTransient<IUserStoreFactory, DefaultUserStoreFactory>();

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:FilesDb"]))
        {
            services.AddUserDbContext<FileBlobUserDb>(options =>
            {
                options.ConnectionString = Path.Combine(configSection["ConnectionStrings:FilesDb"], "users");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:LiteDb"]))
        {
            services.AddUserDbContext<LiteDbUserDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:LiteDb"];
                options.AddDefaults(configSection);
            });
        }
        else
        {
            services.AddUserDbContext<InMemoryUserDb>(options =>
                    options.AddDefaults(configSection)
                );
        }

        #endregion

        #region Add RoleDbContext (optional) 

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:FilesDb"]))
        {
            services.AddRoleDbContext<FileBlobRoleDb>(options =>
            {
                options.ConnectionString = Path.Combine(configSection["ConnectionStrings:FilesDb"], "roles");
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:LiteDb"]))
        {
            services.AddRoleDbContext<LiteDbRoleDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:LiteDb"];
            });
        }
        else
        {
            services.AddRoleDbContext<InMemoryRoleDb>();
        }

        #endregion

        #region Add a ResourceDbContext (required) 

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:FilesDb"]))
        {
            services.AddResourceDbContext<FileBlobResourceDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "resources");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:LiteDb"]))
        {
            services.AddResourceDbContext<LiteDbResourceDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:LiteDb"];
                options.AddDefaults(configSection);
            });
        }
        else
        {
            services.AddResourceDbContext<InMemoryResourceDb>(options =>
                    options.AddDefaults(configSection)
                );
        }


        /*
        services.AddResourceDbContext<MongoBlobResourceDb>(options =>
        {
            options.ConnectionString = configSection["ConnectionStrings:MongoDb"]; //"mongodb://localhost:27017";
        });
        */
        /*
        services.AddResourceDbContext<TableStorageBlobResourceDb>(options =>
        {
            options.ConnectionString = configSection["ConnectionStrings:AzureStorage"];
        });
        */

        #endregion

        #region Add a ClientDbContext (required)

        if (!String.IsNullOrEmpty(configSection["ConnectionStrings:FilesDb"]))
        {
            services.AddClientDbContext<FileBlobClientDb>(options =>
            {
                options.ConnectionString = Path.Combine(SystemInfo.DefaultStoragePath(), "clients");
                options.AddDefaults(configSection);
            });
        }
        else if (!String.IsNullOrEmpty(configSection["ConnectionStrings:LiteDb"]))
        {
            services.AddClientDbContext<LiteDbClientDb>(options =>
            {
                options.ConnectionString = configSection["ConnectionStrings:LiteDb"];
                options.AddDefaults(configSection);
            });
        }
        else
        {
            services.AddClientDbContext<InMemoryClientDb>(options =>
                    options.AddDefaults(configSection)
                );
        }

        /*
        services.AddClientDbContext<MongoBlobClientDb>(options =>
        {
            options.ConnectionString = configSection["ConnectionStrings:MongoDb"]; //"mongodb://localhost:27017";
        });
        */
        /*
        services.AddClientDbContext<TableStorageBlobClientDb>(options =>
        {
            options.ConnectionString = configSection["ConnectionStrings:AzureStorage"];
        });
        */
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

        #region UI (required)

        services.AddUserInterfaceService<DefaultUserInterfaceService>(options =>
        {
            options.ApplicationTitle = configSection["ApplicationTitle"] ?? "IdentityServer.Nova";
            options.OverrideCssContent = Properties.Resources.is4_overrides + Properties.Resources.openid_logo;
            options.MediaContent = new Dictionary<string, byte[]>()
            {
                { "openid-logo.png", Properties.Resources.openid_logo }
            };
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
        else
        {
            services.AddTransient<ICustomEmailSender, NullEmailSender>();
        }

        #endregion

        #region Login BotDetection (optional)

        services.AddLoginBotDetection<LoginBotDetection>();
        services.AddCaptchaRenderer<CaptchaCodeRenderer>();

        #endregion
    }
}
