using Microsoft.Extensions.Configuration;
using System;

namespace IdentityServer.Nova.Clients.Extensions;

static public class ConfigurationExtensions
{
    private const string Authority = "IdentityFromOpenIdConnect:Authority";
    private const string ClientId = "IdentityFromOpenIdConnect:ClientId";
    private const string ClientSecret = "IdentityFromOpenIdConnect:ClientSecret";

    private const string RequireHttpsMetadata = "IdentityFromOpenIdConnect:RequireHttpsMetadata";
    private const string GetClaimsFromUserInfoEndpoint = "IdentityFromOpenIdConnect:GetClaimsFromUserInfoEndpoint";
    private const string SaveTokens = "IdentityFromOpenIdConnect:SaveTokens";

    private const string Scopes = "IdentityFromOpenIdConnect:Scopes";
    private const string NameClaim = "IdentityFromOpenIdConnect:NameClaim";
    private const string RoleClaim = "IdentityFromOpenIdConnect:RoleClaim";
    private const string MapAllClaimActionsExcept = "IdentityFromOpenIdConnect:MapAllClaimActionsExcept";

    /// <summary>
    /// Checks if the identity configuration from OpenIdConnect is properly set.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>True if any required setting is missing, otherwise false.</returns>
    static public bool UseIdentityFromOpenIdConnect(this IConfiguration configuration)
        => string.IsNullOrEmpty(configuration[Authority]) ||
           string.IsNullOrEmpty(configuration[ClientId]) ||
           string.IsNullOrEmpty(configuration[ClientSecret]);

    /// <summary>
    /// Gets the OpenIdConnect authority URL.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The authority URL as a string.</returns>
    static public string OpenIdConnectAuthority(this IConfiguration configuration)
        => configuration[Authority];

    /// <summary>
    /// Gets the OpenIdConnect client ID.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The client ID as a string.</returns>
    static public string OpenIdConnectClientId(this IConfiguration configuration)
        => configuration[ClientId];

    /// <summary>
    /// Gets the OpenIdConnect client secret.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The client secret as a string.</returns>
    static public string OpenIdConnectClientSecret(this IConfiguration configuration)
        => configuration[ClientSecret];

    /// <summary>
    /// Checks if HTTPS metadata is required for OpenIdConnect.
    /// Default: false.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>True if HTTPS metadata is required, otherwise false.</returns>
    static public bool OpenIdConnectRequireHttpsMetadata(this IConfiguration configuration)
        => configuration[RequireHttpsMetadata]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// Checks if claims should be retrieved from the user info endpoint in OpenIdConnect.
    /// Default: true.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>False if claims should not be retrieved, otherwise true.</returns>
    static public bool OpenIdConnectGetClaimsFromUserInfoEndpoint(this IConfiguration configuration)
        => configuration[GetClaimsFromUserInfoEndpoint]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;

    /// <summary>
    /// Checks if tokens should be saved for OpenIdConnect.
    /// Default: true.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>False if tokens should not be saved, otherwise true.</returns>
    static public bool OpenIdConnectSaveTokens(this IConfiguration configuration)
        => configuration[SaveTokens]?.Equals("false", StringComparison.OrdinalIgnoreCase) != true;

    /// <summary>
    /// Gets the array of scopes for OpenIdConnect.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>An array of scope strings. Default: empty array.</returns>
    static public string[] OpenIdConnectScopes(this IConfiguration configuration)
        => configuration[Scopes]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

    /// <summary>
    /// Gets the name claim for OpenIdConnect.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The name claim as a string. Default: null.</returns>
    static public string OpenIdConnectNameClaim(this IConfiguration configuration)
        => configuration[NameClaim];

    /// <summary>
    /// Gets the role claim type for OpenIdConnect.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>The role claim type as a string. Default: null.</returns>
    static public string OpenIdConnectRoleClaimType(this IConfiguration configuration)
        => configuration[RoleClaim];

    /// <summary>
    /// Gets the array of claim actions to be excluded from mapping in OpenIdConnect.
    /// </summary>
    /// <param name="configuration">The configuration instance.</param>
    /// <returns>
    /// An array of claim action strings.
    /// Default: ["iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash"].
    /// </returns>
    static public string[] OpenIdConnectMapAllClaimActionsExcept(this IConfiguration configuration)
        => configuration[MapAllClaimActionsExcept]?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
           ?? new[] { "iss", "nbf", "exp", "aud", "nonce", "iat", "c_hash" };
}
