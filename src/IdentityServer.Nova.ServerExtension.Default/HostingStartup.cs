using IdentityServer.Nova.CaptchaRenderers;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.LiteDb.Services.DbContext;
using IdentityServer.Nova.Reflection;
using IdentityServer.Nova.ServerExtension.Default.Extensions;
using IdentityServer.Nova.ServerExtension.Default.Services.DbContext;
using IdentityServer.Nova.ServerExtension.Default.Services.UI;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.Services.EmailSender;
using IdentityServer.Nova.Services.PasswordHasher;
using IdentityServer.Nova.Services.Security;
using IdentityServer.Nova.Services.Serialize;
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
                options.ConnectionString = @"c:\temp\identityserver_nova\storage\resources";
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
                options.ConnectionString = @"c:\temp\identityserver_nova\storage\resources";
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

        services.AddExportClientDbContext<FileBlobClientExportDb>(options =>
        {
            options.ConnectionString = @"c:\temp\identityserver_nova\storage-export\clients";
            options.CryptoService = new ClearTextCryptoService();
            options.BlobSerializer = new JsonBlobSerializer()
            {
                JsonFormatting = Newtonsoft.Json.Formatting.Indented
            };
        });

        #endregion

        #region Add ExportResourceDbContext (optional)

        services.AddExportResourceDbContext<FileBlobResourceExportDb>(options =>
        {
            options.ConnectionString = @"c:\temp\identityserver_nova\storage-export\resources";
            options.CryptoService = new ClearTextCryptoService();
            options.BlobSerializer = new JsonBlobSerializer()
            {
                JsonFormatting = Newtonsoft.Json.Formatting.Indented
            };
        });

        #endregion

        #region App SecretsVaultDbContext (optional) 

        services.AddSecretsVaultDbContext<FileBlobSecretsVaultDb, SigningCredentialCertStoreCryptoService>(options =>
        {
            options.ConnectionString = @"c:\temp\identityserver_nova\storage\secretsvault";
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
