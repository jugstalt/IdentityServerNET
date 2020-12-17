using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using IdentityServer.Legacy.Extensions.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.Services.PasswordHasher;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer.Legacy.MongoDb.Services.DbContext;
using IdentityServer.Legacy.Reflection;
using IdentityServer.Legacy.UserInteraction;
using IdentityServer.Legacy.Services.EmailSender;
using System.Collections.Generic;
using IdentityServer.Legacy.Azure.Services.DbContext;
using IdentityServer.Legacy.Services.Security;
using IdentityServer.Legacy.CaptchaRenderers;
using IdentityServer4.Models;
using IdentityModel;
using static IdentityModel.OidcConstants;

//[assembly: HostingStartup(typeof(IdentityServer.Legacy.ServerExtension.Test.HostingStartup))]
namespace IdentityServer.Legacy.ServerExtension.Test
{
    [IdentityServerLegacyStartup]
    public class TestHostingStartup : IIdentityServerLegacyStartup
    {
        public void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            #region Add a PasswordHasher (optional)

            services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>();

            #endregion

            #region Add an UserDbContext (required)

            services.AddUserDbContext<FileBlobUserDb>(options =>
            {
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage\users";
                options.CryptoService = new DefaultCryptoService("My super pa33wo4d 1234567890");

                options.ManageAccountEditor = new ManageAccountEditor()
                {
                    AllowDelete = false,
                    ShowChangeEmailPage = true,
                    ShowChangePasswordPage = true,
                    ShowTfaPage = true,
                    EditorInfos = new[]
                    {
                        KnownUserEditorInfos.ReadOnlyUserName(),
                        KnownUserEditorInfos.ReadOnlyEmail(),
                        KnownUserEditorInfos.GivenName(),
                        KnownUserEditorInfos.FamilyName(),
                        KnownUserEditorInfos.Organisation(),
                        KnownUserEditorInfos.PhoneNumber(),
                        KnownUserEditorInfos.BirthDate(),
                        new EditorInfo("Ranking", typeof(int)) { Category="Advanced", ClaimName="ranking" },
                        new EditorInfo("Cost", typeof(double)) { Category="Advanced", ClaimName="cost" },
                        new EditorInfo("SendInfos", typeof(bool)) { Category="Privacy", ClaimName="send_infos"}
                    }
                };

                options.AdminAccountEditor = new AdminAccountEditor()
                {
                    AllowDelete = true,
                    AllowSetPassword = true,
                    EditorInfos = new[]
                      {
                        KnownUserEditorInfos.EditableEmail(),
                        KnownUserEditorInfos.GivenName(),
                        KnownUserEditorInfos.FamilyName(),
                        new EditorInfo("Ranking", typeof(int)) { Category="Advanced", ClaimName="ranking" },
                        new EditorInfo("Cost", typeof(double)) { Category="Advanced", ClaimName="cost" },
                      }
                };
            });

            #endregion

            #region Add RoleDbContext (optional) 

            //services.AddRoleDbContext<InMemoryRoleDb>();
            services.AddRoleDbContext<FileBlobRoleDb>(options =>
            {
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage\roles";
            });

            #endregion

            #region Add a ResourceDbContext (required) 

            
            services.AddResourceDbContext<FileBlobResourceDb>(options =>
            {
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage\resources";
                options.CryptoService = new Base64CryptoService();
                options.InitialApiResources = new ApiResource[]
                {
                    new ApiResource("api1","My Api1"),
                    new ApiResource("api2","My Api2")
                };
                options.InitialIdentityResources = new IdentityResource[]
                {

                };
            });
            

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

            services.AddClientDbContext<FileBlobClientDb>(options =>
            {
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage\clients";
                options.CryptoService = new Base64CryptoService();
                options.IntialClients = new Client[]
                {
                    new Client()
                    {
                        ClientId = "client",
                        ClientSecrets = new Secret[]
                        {
                            new Secret()
                            {
                                Value = "secret1".ToSha256()
                            },
                            new Secret()
                            {
                                Value = "secret2".ToSha256()
                            }
                        },
                        AllowedGrantTypes = { GrantTypes.ClientCredentials, GrantTypes.Password },
                        AllowedScopes = { "api1", "api2", "profile", "openid" }
                    },
                    new Client()
                    {
                        ClientId = "mvc",
                        ClientSecrets = new Secret[]
                        {
                            new Secret() { Value ="secret".ToSha256() }
                        },
                        AllowedGrantTypes = { GrantTypes.AuthorizationCode },
                        AllowedScopes = { "openid", "profile", "api1", "email", "role", "address", "phone" },

                        AlwaysSendClientClaims = true,
                        AlwaysIncludeUserClaimsInIdToken = true,

                        AllowOfflineAccess = true,
                        RequireConsent = false,
                        RequirePkce = true,

                        RedirectUris = new string[] { "https://localhost:44356/signin-oidc" },
                        PostLogoutRedirectUris = new string[] { "https://localhost:44356/signout-callback-oidc" }
                    }
                };
            });

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
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage-export\clients";
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
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage-export\resources";
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
                options.ConnectionString = @"c:\temp\identityserver_legacy\storage\secretsvault";
                options.CryptoService = new Base64CryptoService();
            });

            #endregion

            #region UI (required)

            services.Configure<UserInterfaceConfiguration>(options =>
            {
                options.ApplicationTitle = "IdentityServer.Legacy.Test";  // required
                options.OverrideCssContent = Properties.Resources.is4_overrides;
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
}
