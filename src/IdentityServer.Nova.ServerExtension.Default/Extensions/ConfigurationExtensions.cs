using IdentityModel;
using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.UserInteraction;
using Microsoft.Extensions.Configuration;
using System;
using static IdentityModel.OidcConstants;

namespace IdentityServer.Nova.ServerExtension.Default.Extensions;
internal static class ConfigurationExtensions
{
    public static void AddDefaults(this UserDbContextConfiguration options,
                                   IConfiguration config)
    {
        options.CryptoService = !String.IsNullOrEmpty(config["BlobCryptoKey"])
            ? new DefaultCryptoService(config["BlobCryptoKey"])
            : new Base64CryptoService();

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
                    //KnownUserEditorInfos.Organisation(),
                    //KnownUserEditorInfos.PhoneNumber(),
                    //KnownUserEditorInfos.BirthDate(),
                    //new EditorInfo("Ranking", typeof(int)) { Category="Advanced", ClaimName="ranking" },
                    //new EditorInfo("Cost", typeof(double)) { Category="Advanced", ClaimName="cost" },
                    //new EditorInfo("SendInfos", typeof(bool)) { Category="Privacy", ClaimName="send_infos"}
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
                    //new EditorInfo("Ranking", typeof(int)) { Category="Advanced", ClaimName="ranking" },
                    //new EditorInfo("Cost", typeof(double)) { Category="Advanced", ClaimName="cost" },
            }
        };
    }

    public static void AddDefaults(this ResourceDbContextConfiguration options,
                                   IConfiguration config)
    {
        options.CryptoService = !String.IsNullOrEmpty(config["BlobCryptoKey"])
            ? new DefaultCryptoService(config["BlobCryptoKey"])
            : new Base64CryptoService();

        options.InitialApiResources = new ApiResourceModel[]
        {
                    //new ApiResourceModel("api1","My Api1"),
                    //new ApiResourceModel("api2","My Api2")
        };
        options.InitialIdentityResources = new IdentityResourceModel[]
        {

        };
    }  

    public static void AddDefaults(this ClientDbContextConfiguration options,
                                   IConfiguration config)
    {
        options.CryptoService = !String.IsNullOrEmpty(config["BlobCryptoKey"])
            ? new DefaultCryptoService(config["BlobCryptoKey"])
            : new Base64CryptoService();

        options.IntialClients = new ClientModel[]
        {
                //new ClientModel()
                //{
                //    ClientId = "client",
                //    ClientSecrets = new SecretModel[]
                //    {
                //        new SecretModel()
                //        {
                //            Value = "secret1".ToSha256()
                //        },
                //        new SecretModel()
                //        {
                //            Value = "secret2".ToSha256()
                //        }
                //    },
                //    AllowedGrantTypes = { GrantTypes.ClientCredentials, GrantTypes.Password },
                //    AllowedScopes = { "api1", "api2", "profile", "openid" }
                //},
                //new ClientModel()
                //{
                //    ClientId = "mvc",
                //    ClientSecrets = new SecretModel[]
                //    {
                //        new SecretModel() { Value ="secret".ToSha256() }
                //    },
                //    AllowedGrantTypes = { GrantTypes.AuthorizationCode },
                //    AllowedScopes = { "openid", "profile", "api1", "email", "role", "address", "phone" },

                //    AlwaysSendClientClaims = true,
                //    AlwaysIncludeUserClaimsInIdToken = true,

                //    AllowOfflineAccess = true,
                //    RequireConsent = false,
                //    RequirePkce = true,

                //    RedirectUris = new string[] { "https://localhost:44356/signin-oidc" },
                //    PostLogoutRedirectUris = new string[] { "https://localhost:44356/signout-callback-oidc" }
                //}
        };
    }
}
