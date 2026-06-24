using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

public static class IdentityServerNetConfigurationBuilderExtensions
{
    internal const string ConfigPrefix = "IdentityServer__";

    #region Application

    /// <summary>
    /// Sets the application title shown in the navbar and login page.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder WithApplicationTitle(
        this IdentityServerNetConfigurationBuilder builder,
        string title)
        => builder.SetConfigValue($"{ConfigPrefix}ApplicationTitle", title);

    /// <summary>
    /// Sets the public origin URL (scheme + host + optional port), e.g. "https://auth.example.com".
    /// Required when running behind a reverse proxy.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder WithPublicOrigin(
        this IdentityServerNetConfigurationBuilder builder,
        string origin)
        => builder.SetConfigValue($"{ConfigPrefix}PublicOrigin", origin);

    #endregion

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

    #region Passkey (WebAuthn / FIDO2)

    /// <summary>
    /// Enables passwordless first-factor sign-in via passkey.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder AllowPasskeyPasswordless(
        this IdentityServerNetConfigurationBuilder builder,
        bool allow = true)
        => builder.SetConfigValue($"{ConfigPrefix}Login__Passkey__AllowPasswordless", allow);

    /// <summary>
    /// Enables passkey as a second factor after username/password.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder AllowPasskeySecondFactor(
        this IdentityServerNetConfigurationBuilder builder,
        bool allow = true)
        => builder.SetConfigValue($"{ConfigPrefix}Login__Passkey__AllowSecondFactor", allow);

    /// <summary>
    /// WebAuthn Relying Party ID — must match the effective domain of your deployment
    /// (e.g. "example.com"). Leave empty to inherit from the request host.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder WithPasskeyServerDomain(
        this IdentityServerNetConfigurationBuilder builder,
        string domain)
        => builder.SetConfigValue($"{ConfigPrefix}Login__Passkey__ServerDomain", domain);

    /// <summary>
    /// Human-readable name shown in the authenticator during passkey registration.
    /// </summary>
    public static IdentityServerNetConfigurationBuilder WithPasskeyRelyingPartyName(
        this IdentityServerNetConfigurationBuilder builder,
        string name)
        => builder.SetConfigValue($"{ConfigPrefix}Login__Passkey__RelyingPartyName", name);

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

    #region Admin UI

    /// <summary>Hides the Users admin section.</summary>
    public static IdentityServerNetConfigurationBuilder DenyAdminUsers(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenyAdminUsers", deny);

    /// <summary>Hides the Roles admin section.</summary>
    public static IdentityServerNetConfigurationBuilder DenyAdminRoles(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenyAdminRoles", deny);

    /// <summary>Hides the Resources (API/Identity) admin section.</summary>
    public static IdentityServerNetConfigurationBuilder DenyAdminResources(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenyAdminResources", deny);

    /// <summary>Hides the Clients admin section.</summary>
    public static IdentityServerNetConfigurationBuilder DenyAdminClients(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenyAdminClients", deny);

    /// <summary>Hides the Signing Credentials admin UI.</summary>
    public static IdentityServerNetConfigurationBuilder DenySigningUI(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenySigningUI", deny);

    /// <summary>Prevents admins from creating new signing certificates.</summary>
    public static IdentityServerNetConfigurationBuilder DenyAdminCreateCerts(
        this IdentityServerNetConfigurationBuilder builder,
        bool deny = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__DenyAdminCreateCerts", deny);

    /// <summary>Enables the Data Transfer section (import/export).</summary>
    public static IdentityServerNetConfigurationBuilder AllowDataTransfer(
        this IdentityServerNetConfigurationBuilder builder,
        bool allow = true)
        => builder.SetConfigValue($"{ConfigPrefix}Admin__AllowDataTransfer", allow);

    #endregion

    #region Helper

    private static IdentityServerNetConfigurationBuilder SetConfigValue(
        this IdentityServerNetConfigurationBuilder builder,
        string key,
        bool value)
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
