using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.UserInteraction;
using Microsoft.Extensions.Configuration;
using System;

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
    }
}
