using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class IdentityServerNetConfigurationBuilderExtensions
{
    internal const string ConfigPrefix = "IdentityServer__";

    #region Login

    public static IdentityServerNetConfigurationBuilder DenyLocalLogin(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Login__DenyLocalLogin", deny);

    public static IdentityServerNetConfigurationBuilder DenyForgotPasswordChallange(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Login__DenyForgotPasswordChallange", deny);

    public static IdentityServerNetConfigurationBuilder DenyRememberLogin(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Login__DenyRememberLogin", deny);

    public static IdentityServerNetConfigurationBuilder RememberLoginDefaultValue(
        this IdentityServerNetConfigurationBuilder builder,
        bool value)
        => builder.SetConfigValue($"{ConfigPrefix}Login__RememberLoginDefaultValue", value);

    #endregion

    #region Account

    public static IdentityServerNetConfigurationBuilder DenyManageAccount(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Account__DenyManageAccount", deny);

    public static IdentityServerNetConfigurationBuilder DenyRegisterAccount(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Account__DenyRegisterAccount", deny);

    #endregion

    #region Helper

    private static IdentityServerNetConfigurationBuilder SetConfigValue(
        this IdentityServerNetConfigurationBuilder builder,
        string key,
        bool value = true)
        => builder.SetConfigValue(key, value.ToString().ToLowerInvariant());

    private static IdentityServerNetConfigurationBuilder SetConfigValue(
        this IdentityServerNetConfigurationBuilder builder,
        string key,
        string value)
    {
        builder.ResourceBuilder.WithEnvironment(
            e => e.EnvironmentVariables.Add(key, value));

        return builder;
    }

    #endregion
}
