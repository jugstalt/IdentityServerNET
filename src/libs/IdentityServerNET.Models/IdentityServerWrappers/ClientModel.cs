using IdentityServer4.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Claims;

namespace IdentityServerNET.Models.IdentityServerWrappers;

public class ClientModel
{
    public ClientModel()
    {
        this.AllowedGrantTypes = new List<string>();
        this.IdentityProviderRestrictions = new List<string>();
        this.Claims = new List<Claim>();
        this.AllowedScopes = new List<string>();
        this.AllowedUserDomains = new List<string>();
        this.Properties = new Dictionary<string, string>();
        this.ClientSecrets = new List<SecretModel>();
        this.AllowedCorsOrigins = new List<string>();
        this.AllowedGrantTypes = new List<string>();
        this.RedirectUris = new List<string>();
        this.PostLogoutRedirectUris = new List<string>();

        this.Enabled = true;
        this.EnableLocalLogin = true;
        this.ProtocolType = "oidc";

        this.IdentityTokenLifetime = 300;
        this.AccessTokenLifetime = 3600;
        this.AuthorizationCodeLifetime = 300;
        this.ClientClaimsPrefix = "client_";
        this.AbsoluteRefreshTokenLifetime = 2592000;
        this.SlidingRefreshTokenLifetime = 1296000;
        this.DeviceCodeLifetime = 300;

        this.FrontChannelLogoutSessionRequired = this.BackChannelLogoutSessionRequired = true;
    }

    [JsonProperty("AllowOfflineAccess")]
    [Description("Allows clients to request refresh tokens for offline access.")]
    public bool AllowOfflineAccess { get; set; }

    [JsonProperty("IdentityTokenLifetime")]
    public int IdentityTokenLifetime { get; set; }

    [JsonProperty("AccessTokenLifetime")]
    public int AccessTokenLifetime { get; set; }

    [JsonProperty("AuthorizationCodeLifetime")]
    public int AuthorizationCodeLifetime { get; set; }

    [JsonProperty("AbsoluteRefreshTokenLifetime")]
    public int AbsoluteRefreshTokenLifetime { get; set; }

    [JsonProperty("SlidingRefreshTokenLifetime")]
    public int SlidingRefreshTokenLifetime { get; set; }

    [JsonProperty("ConsentLifetime")]
    public int? ConsentLifetime { get; set; }

    [JsonProperty("RefreshTokenUsage")]
    public TokenUsage RefreshTokenUsage { get; set; }

    [JsonProperty("UpdateAccessTokenClaimsOnRefresh")]
    [Description("Re-issues updated access token claims when a refresh token is used.")]
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

    [JsonProperty("RefreshTokenExpiration")]
    public TokenExpiration RefreshTokenExpiration { get; set; }

    [JsonProperty("AccessTokenType")]
    public AccessTokenType AccessTokenType { get; set; }

    [JsonProperty("EnableLocalLogin")]
    [Description("Allows users to log in with a local username and password.")]
    public bool EnableLocalLogin { get; set; }

    [JsonProperty("IdentityProviderRestrictions")]
    public ICollection<string> IdentityProviderRestrictions { get; set; }

    [JsonProperty("IncludeJwtId")]
    [Description("Adds a unique jti claim to each JWT access token for tracking.")]
    public bool IncludeJwtId { get; set; }

    [JsonProperty("Claims")]
    public ICollection<Claim> Claims { get; set; }

    [JsonProperty("AlwaysSendClientClaims")]
    [Description("Always includes client claims in the token, even without user interaction.")]
    public bool AlwaysSendClientClaims { get; set; }

    [JsonProperty("ClientClaimsPrefix")]
    public string ClientClaimsPrefix { get; set; }

    [JsonProperty("PairWiseSubjectSalt")]
    public string PairWiseSubjectSalt { get; set; } = "";

    [JsonProperty("UserSsoLifetime")]
    public int? UserSsoLifetime { get; set; }

    [JsonProperty("UserCodeType")]
    public string? UserCodeType { get; set; } = null;

    [JsonProperty("DeviceCodeLifetime")]
    public int DeviceCodeLifetime { get; set; }

    [JsonProperty("AlwaysIncludeUserClaimsInIdToken")]
    [Description("Includes all requested user claims directly in the identity token.")]
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

    [JsonProperty("AllowedScopes")]
    public ICollection<string> AllowedScopes { get; set; }

    [JsonProperty("AllowedUserDomains")]
    [Description("Additional user e-mail domains allowed to sign in to this (realm) client. '*' allows all users.")]
    public ICollection<string> AllowedUserDomains { get; set; }

    [JsonProperty("Properties")]
    public IDictionary<string, string> Properties { get; set; }

    [JsonProperty("BackChannelLogoutSessionRequired")]
    [Description("Sends the session ID in back-channel logout notifications.")]
    public bool BackChannelLogoutSessionRequired { get; set; }

    [JsonProperty("Enabled")]
    [Description("Enables or disables this client entirely.")]
    public bool Enabled { get; set; }

    [JsonProperty("ClientId")]
    public string ClientId { get; set; } = "";

    [JsonProperty("ProtocolType")]
    public string ProtocolType { get; set; }

    [JsonProperty("ClientSecrets")]
    public ICollection<SecretModel> ClientSecrets { get; set; }

    [JsonProperty("RequireClientSecret")]
    [Description("Requires a client secret for token endpoint requests.")]
    public bool RequireClientSecret { get; set; }

    [JsonProperty("ClientName")]
    public string ClientName { get; set; } = "";

    [JsonProperty("Description")]
    public string Description { get; set; } = "";

    [JsonProperty("ClientUri")]
    public string ClientUri { get; set; } = "";

    [JsonProperty("LogoUri")]
    public string LogoUri { get; set; } = "";

    [JsonProperty("AllowedCorsOrigins")]
    public ICollection<string> AllowedCorsOrigins { get; set; }

    [JsonProperty("RequireConsent")]
    [Description("Displays the consent screen to users before authorizing the client.")]
    public bool RequireConsent { get; set; }

    [JsonProperty("AllowedGrantTypes")]
    public ICollection<string> AllowedGrantTypes { get; set; }

    [JsonProperty("RequirePkce")]
    [Description("Enforces Proof Key for Code Exchange (PKCE) for authorization code flows.")]
    public bool RequirePkce { get; set; }

    [JsonProperty("AllowPlainTextPkce")]
    [Description("Permits the less secure plain text code challenge method for PKCE.")]
    public bool AllowPlainTextPkce { get; set; }

    [JsonProperty("RequirePushedAuthorization")]
    [Description("Requires authorization requests to use Pushed Authorization Requests (PAR).")]
    public bool RequirePushedAuthorization { get; set; }

    [JsonProperty("AllowAccessTokensViaBrowser")]
    [Description("Allows access tokens to be returned in browser URL fragments (implicit flow).")]
    public bool AllowAccessTokensViaBrowser { get; set; }

    [JsonProperty("RedirectUris")]
    public ICollection<string> RedirectUris { get; set; }

    [JsonProperty("PostLogoutRedirectUris")]
    public ICollection<string> PostLogoutRedirectUris { get; set; }

    [JsonProperty("FrontChannelLogoutUri")]
    public string FrontChannelLogoutUri { get; set; } = "";

    [JsonProperty("FrontChannelLogoutSessionRequired")]
    [Description("Includes the session ID in front-channel logout iframe requests.")]
    public bool FrontChannelLogoutSessionRequired { get; set; }

    [JsonProperty("BackChannelLogoutUri")]
    public string BackChannelLogoutUri { get; set; } = "";

    [JsonProperty("AllowRememberConsent")]
    [Description("Lets users save their consent decision to skip the consent screen next time.")]
    public bool AllowRememberConsent { get; set; }

    // Consent screen logo — stored as base64, served via /ui/client-logo/{id}
    [JsonProperty("LogoBase64")]
    public string? LogoBase64 { get; set; }

    [JsonProperty("LogoMimeType")]
    public string? LogoMimeType { get; set; }

    [JsonIgnore]
    public bool HasLogoImage => !string.IsNullOrEmpty(LogoBase64);

    [JsonIgnore]
    public Client IdentityServer4Instance
    {
        get
        {
            return new Client()
            {

                AllowOfflineAccess = this.AllowOfflineAccess,
                IdentityTokenLifetime = this.IdentityTokenLifetime,
                AccessTokenLifetime = this.AccessTokenLifetime,
                AuthorizationCodeLifetime = this.AuthorizationCodeLifetime,
                AbsoluteRefreshTokenLifetime = this.AbsoluteRefreshTokenLifetime,
                SlidingRefreshTokenLifetime = this.SlidingRefreshTokenLifetime,
                ConsentLifetime = this.ConsentLifetime,
                RefreshTokenUsage = this.RefreshTokenUsage,
                UpdateAccessTokenClaimsOnRefresh = this.UpdateAccessTokenClaimsOnRefresh,
                RefreshTokenExpiration = this.RefreshTokenExpiration,
                AccessTokenType = this.AccessTokenType,
                EnableLocalLogin = this.EnableLocalLogin,
                IdentityProviderRestrictions = this.IdentityProviderRestrictions,
                Claims = this.Claims?
                                .Select(c => new ClientClaim(c.Type, c.Value, c.ValueType))
                                .ToList(),
                AlwaysSendClientClaims = this.AlwaysSendClientClaims,
                ClientClaimsPrefix = this.ClientClaimsPrefix,
                PairWiseSubjectSalt = this.PairWiseSubjectSalt,
                UserSsoLifetime = this.UserSsoLifetime,
                UserCodeType = this.UserCodeType,
                DeviceCodeLifetime = this.DeviceCodeLifetime,
                AlwaysIncludeUserClaimsInIdToken = this.AlwaysIncludeUserClaimsInIdToken,
                AllowedScopes = this.AllowedScopes,
                Properties = this.AllowedUserDomains is { Count: > 0 }
                    ? new Dictionary<string, string>(this.Properties ?? new Dictionary<string, string>())
                      {
                          [Extensions.RealmConventionExtensions.AllowedUserDomainsProperty] = string.Join(" ", this.AllowedUserDomains)
                      }
                    : this.Properties,
                BackChannelLogoutSessionRequired = this.BackChannelLogoutSessionRequired,
                Enabled = this.Enabled,
                ClientId = this.ClientId,
                ProtocolType = this.ProtocolType,
                ClientSecrets = this.ClientSecrets?.Select(s => s.IdentityServer4Instance).ToList(),
                RequireClientSecret = this.RequireClientSecret,
                ClientName = this.ClientName,
                Description = this.Description,
                ClientUri = this.ClientUri,
                LogoUri = this.LogoUri,
                AllowedCorsOrigins = this.AllowedCorsOrigins,
                RequireConsent = this.RequireConsent,
                AllowedGrantTypes = this.AllowedGrantTypes,
                RequirePkce = this.RequirePkce,
                AllowPlainTextPkce = this.AllowPlainTextPkce,
                AllowAccessTokensViaBrowser = this.AllowAccessTokensViaBrowser,
                RedirectUris = this.RedirectUris,
                PostLogoutRedirectUris = this.PostLogoutRedirectUris,
                FrontChannelLogoutUri = this.FrontChannelLogoutUri,
                FrontChannelLogoutSessionRequired = this.FrontChannelLogoutSessionRequired,
                BackChannelLogoutUri = this.BackChannelLogoutUri,
                AllowRememberConsent = this.AllowRememberConsent,
                RequirePushedAuthorization = this.RequirePushedAuthorization
            };
        }
    }
}
