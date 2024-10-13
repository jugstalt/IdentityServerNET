using IdentityServer4.Models;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace IdentityServer.Nova.Models.IdentityServerWrappers;

public class ClientModel
{
    public ClientModel()
    {
        this.AllowedGrantTypes = new List<string>();
        this.IdentityProviderRestrictions = new List<string>();
        this.Claims = new List<Claim>();
        this.AllowedScopes = new List<string>();
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
    public bool UpdateAccessTokenClaimsOnRefresh { get; set; }

    [JsonProperty("RefreshTokenExpiration")]
    public TokenExpiration RefreshTokenExpiration { get; set; }

    [JsonProperty("AccessTokenType")]
    public AccessTokenType AccessTokenType { get; set; }

    [JsonProperty("EnableLocalLogin")]
    public bool EnableLocalLogin { get; set; }

    [JsonProperty("IdentityProviderRestrictions")]
    public ICollection<string> IdentityProviderRestrictions { get; set; }

    [JsonProperty("IncludeJwtId")]
    public bool IncludeJwtId { get; set; }

    [JsonProperty("Claims")]
    public ICollection<Claim> Claims { get; set; }

    [JsonProperty("AlwaysSendClientClaims")]
    public bool AlwaysSendClientClaims { get; set; }

    [JsonProperty("ClientClaimsPrefix")]
    public string ClientClaimsPrefix { get; set; }

    [JsonProperty("PairWiseSubjectSalt")]
    public string PairWiseSubjectSalt { get; set; } = "";

    [JsonProperty("UserSsoLifetime")]
    public int? UserSsoLifetime { get; set; }

    [JsonProperty("UserCodeType")]
    public string UserCodeType { get; set; } = "";

    [JsonProperty("DeviceCodeLifetime")]
    public int DeviceCodeLifetime { get; set; }

    [JsonProperty("AlwaysIncludeUserClaimsInIdToken")]
    public bool AlwaysIncludeUserClaimsInIdToken { get; set; }

    [JsonProperty("AllowedScopes")]
    public ICollection<string> AllowedScopes { get; set; }

    [JsonProperty("Properties")]
    public IDictionary<string, string> Properties { get; set; }

    [JsonProperty("BackChannelLogoutSessionRequired")]
    public bool BackChannelLogoutSessionRequired { get; set; }

    [JsonProperty("Enabled")]
    public bool Enabled { get; set; }

    [JsonProperty("ClientId")]
    public string ClientId { get; set; } = "";

    [JsonProperty("ProtocolType")]
    public string ProtocolType { get; set; }

    [JsonProperty("ClientSecrets")]
    public ICollection<SecretModel> ClientSecrets { get; set; }

    [JsonProperty("RequireClientSecret")]
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
    public bool RequireConsent { get; set; }

    [JsonProperty("AllowedGrantTypes")]
    public ICollection<string> AllowedGrantTypes { get; set; }

    [JsonProperty("RequirePkce")]
    public bool RequirePkce { get; set; }

    [JsonProperty("AllowPlainTextPkce")]
    public bool AllowPlainTextPkce { get; set; }

    [JsonProperty("AllowAccessTokensViaBrowser")]
    public bool AllowAccessTokensViaBrowser { get; set; }

    [JsonProperty("RedirectUris")]
    public ICollection<string> RedirectUris { get; set; }

    [JsonProperty("PostLogoutRedirectUris")]
    public ICollection<string> PostLogoutRedirectUris { get; set; }

    [JsonProperty("FrontChannelLogoutUri")]
    public string FrontChannelLogoutUri { get; set; } = "";

    [JsonProperty("FrontChannelLogoutSessionRequired")]
    public bool FrontChannelLogoutSessionRequired { get; set; }

    [JsonProperty("BackChannelLogoutUri")]
    public string BackChannelLogoutUri { get; set; } = "";

    [JsonProperty("AllowRememberConsent")]
    public bool AllowRememberConsent { get; set; }

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
                Properties = this.Properties,
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
                AllowRememberConsent = this.AllowRememberConsent
            };
        }
    }
}
