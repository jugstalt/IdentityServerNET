using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer.Legacy.Services.DbContext;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer4.Models;
using static IdentityModel.OidcConstants;
using IdentityModel;
using IdentityServer.Legacy.Services.PasswordHasher;
using IdentityServer.Legacy.Services.Serialize;
using IdentityServer.Legacy.MongoDb.Services.DbContext;

[assembly: HostingStartup(typeof(IdentityServer.Legacy.ServerExtension.Test.HostingStartup))]
namespace IdentityServer.Legacy.ServerExtension.Test
{
    public class HostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.ConfigureServices((context, services) =>
            {
                #region Add a PasswordHasher (optional)

                services.AddTransient<IPasswordHasher<ApplicationUser>, Sha512PasswordHasher>();

                #endregion

                #region Add an UserDbContext (required)

                services.AddUserDbContext<FileBlobUserDb>(options =>
                {
                    options.ConnectionString = @"c:\temp\identityserver_legacy\storage\users";
                    options.CryptoService = new DefaultCryptoService("My super pa33wo4d 1234567890");
                });

                #endregion

                #region Add a ResourceDbContext (required) 

                /*
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
                */

                services.AddResourceDbContext<MongoBlobResourceDb>(options =>
                {
                    options.ConnectionString = "mongodb://localhost:27017";
                });

                #endregion

                #region Add a ClientDbContext (required)

                /*
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
                */

                services.AddClientDbContext<MongoBlobClientDb>(options =>
                {
                    options.ConnectionString = "mongodb://localhost:27017";
                });

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
            });
        }
    }
}
