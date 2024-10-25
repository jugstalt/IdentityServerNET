using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.UserInteraction;
using Microsoft.Extensions.Configuration;

namespace IdentityServerNET.Extensions;

public static class ConfigurationExtensions
{
    public static void AddDefaults(this UserDbContextConfiguration options,
                                   IConfiguration config)
    {
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
        //options.InitialApiResources = new ApiResourceModel[]
        //{
        //};
        //options.InitialIdentityResources = new IdentityResourceModel[]
        //{

        //};
    }

    public static void AddDefaults(this ClientDbContextConfiguration options,
                                   IConfiguration config)
    {
        //options.IntialClients = new ClientModel[]
        //{

        //};
    }
}
