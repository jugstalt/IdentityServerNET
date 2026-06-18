using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Tests the lifecycle and integrity of issued tokens: the identity token must be a valid JWT
/// signed by the server's published JWKS with the expected issuer/audience, refresh tokens must
/// mint new access tokens, and the UserInfo endpoint must return the subject's claims only for a
/// valid access token.
/// </summary>
public class TokenLifecycleTests
{
    private const string ClientId = "code-client";
    private const string ClientSecret = "code-secret";
    private const string RedirectUri = "https://client/callback";
    private const string SubjectId = "alice";

    // A machine-to-machine client used for the deterministic expiry tests (short-lived reference
    // tokens whose lifetime is validated against the injected test clock).
    private const string ShortLivedClientId = "short-lived";
    private const string ShortLivedClientSecret = "short-lived-secret";
    private const string ApiSecret = "api-secret";
    private const int ShortLifetimeSeconds = 60;

    private readonly OidcTestPipeline _pipeline = new();

    public TokenLifecycleTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = ClientId,
            ClientSecrets = { new Secret(ClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireConsent = false,
            RedirectUris = { RedirectUri },
            AllowedScopes = { "openid", "profile", "api" },
            AllowOfflineAccess = true
        });

        _pipeline.Users.Add(new TestUser
        {
            SubjectId = SubjectId,
            Username = "alice",
            Password = "Pass123$",
            Claims = new[] { new System.Security.Claims.Claim("name", "Alice Smith") }
        });

        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.IdentityScopes.Add(new IdentityResources.Profile());
        _pipeline.ApiScopes.Add(new ApiScope("api"));

        // Register an API resource so that issued access tokens carry an audience ("aud") claim,
        // which lets the audience/scope assertions below verify token targeting. The API secret
        // lets the introspection endpoint authenticate for the reference-token expiry test.
        _pipeline.ApiResources.Add(new ApiResource("api")
        {
            Scopes = { "api" },
            ApiSecrets = { new Secret(ApiSecret.Sha256()) }
        });

        // A client that issues SHORT-LIVED REFERENCE access tokens. Reference tokens validate their
        // expiry against the injected ISystemClock, so advancing the test clock proves
        // deterministically that an expired token is rejected.
        _pipeline.Clients.Add(new Client
        {
            ClientId = ShortLivedClientId,
            ClientSecrets = { new Secret(ShortLivedClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { "api" },
            AccessTokenType = AccessTokenType.Reference,
            AccessTokenLifetime = ShortLifetimeSeconds
        });

        _pipeline.Initialize();
    }

    private static string CreateCodeVerifier() =>
        Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string CreateCodeChallenge(string verifier) =>
        Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    /// <summary>Drives login + authorize + token and returns the parsed token response.</summary>
    private async Task<JsonElement> GetTokensAsync(string scope = "openid profile api offline_access")
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var challenge = CreateCodeChallenge(verifier);

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var authorizeResponse = await _pipeline.BrowserClient.GetAsync(
            $"{OidcTestPipeline.AuthorizeEndpoint}?client_id={ClientId}&response_type=code" +
            $"&scope={Uri.EscapeDataString(scope)}&redirect_uri={Uri.EscapeDataString(RedirectUri)}" +
            $"&state=s&nonce=n&code_challenge={challenge}&code_challenge_method=S256");

        var code = QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();

        var tokenResponse = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = verifier
            }));

        var json = await tokenResponse.Content.ReadAsStringAsync();
        return JsonDocument.Parse(json).RootElement.Clone();
    }

    private async Task<JsonWebKeySet> GetJwksAsync()
    {
        var jwksJson = await _pipeline.BackChannelClient.GetStringAsync(OidcTestPipeline.DiscoveryKeysEndpoint);
        return new JsonWebKeySet(jwksJson);
    }

    [Fact]
    public async Task IdentityToken_IsSignedByPublishedJwks_AndHasExpectedClaims()
    {
        var tokens = await GetTokensAsync();
        var idToken = tokens.GetProperty("id_token").GetString()!;

        var jwks = await GetJwksAsync();

        var parameters = new TokenValidationParameters
        {
            ValidIssuer = OidcTestPipeline.BaseUrl,
            ValidAudience = ClientId,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };

        var handler = new JwtSecurityTokenHandler();
        // Preserve original JWT claim types ("sub" instead of the mapped .NET URI).
        handler.MapInboundClaims = false;
        var principal = handler.ValidateToken(idToken, parameters, out var validatedToken);

        Assert.NotNull(principal);
        var jwt = Assert.IsType<JwtSecurityToken>(validatedToken);
        Assert.Equal("RS256", jwt.SignatureAlgorithm);
        Assert.Equal(SubjectId, principal.FindFirst("sub")?.Value);
    }

    [Fact]
    public async Task TamperedIdentityToken_FailsSignatureValidation()
    {
        var tokens = await GetTokensAsync();
        var idToken = tokens.GetProperty("id_token").GetString()!;

        // Flip the last character of the signature segment to corrupt it.
        var parts = idToken.Split('.');
        var sig = parts[2];
        parts[2] = sig.Substring(0, sig.Length - 1) + (sig[^1] == 'A' ? 'B' : 'A');
        var tampered = string.Join('.', parts);

        var jwks = await GetJwksAsync();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = OidcTestPipeline.BaseUrl,
            ValidAudience = ClientId,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(tampered, parameters, out _));
    }

    [Fact]
    public async Task RefreshToken_IssuesNewAccessToken()
    {
        var tokens = await GetTokensAsync();
        var refreshToken = tokens.GetProperty("refresh_token").GetString()!;
        var originalAccessToken = tokens.GetProperty("access_token").GetString()!;

        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["refresh_token"] = refreshToken
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.False(root.TryGetProperty("error", out _));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("access_token").GetString()));
    }

    [Fact]
    public async Task UserInfo_WithValidAccessToken_ReturnsSubjectClaims()
    {
        var tokens = await GetTokensAsync("openid profile");
        var accessToken = tokens.GetProperty("access_token").GetString()!;

        var request = new HttpRequestMessage(HttpMethod.Get, OidcTestPipeline.UserInfoEndpoint);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _pipeline.BackChannelClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(SubjectId, doc.RootElement.GetProperty("sub").GetString());
    }

    [Fact]
    public async Task UserInfo_WithoutAccessToken_IsUnauthorized()
    {
        var response = await _pipeline.BackChannelClient.GetAsync(OidcTestPipeline.UserInfoEndpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AccessToken_HasExpectedAudienceScopeIssuerAndClient()
    {
        var tokens = await GetTokensAsync("openid api");
        var accessToken = tokens.GetProperty("access_token").GetString()!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);

        // Audience must target the API resource so a token for this API cannot be replayed elsewhere.
        Assert.Contains("api", jwt.Audiences);
        // The granted scope must be present...
        Assert.Contains(jwt.Claims, c => c.Type == "scope" && c.Value == "api");
        // ...and bound to the requesting client and this issuer.
        Assert.Equal(ClientId, jwt.Claims.FirstOrDefault(c => c.Type == "client_id")?.Value);
        Assert.Equal(OidcTestPipeline.BaseUrl, jwt.Issuer);
    }

    [Fact]
    public async Task IdentityToken_ContainsRequestedNonce()
    {
        // GetTokensAsync sends nonce=n; the id_token must mirror it to defend against replay.
        var tokens = await GetTokensAsync("openid api");
        var idToken = tokens.GetProperty("id_token").GetString()!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
        Assert.Equal("n", jwt.Claims.FirstOrDefault(c => c.Type == "nonce")?.Value);
    }

    [Fact]
    public async Task IdentityToken_AtHash_MatchesAccessToken()
    {
        var tokens = await GetTokensAsync("openid api");
        var idToken = tokens.GetProperty("id_token").GetString()!;
        var accessToken = tokens.GetProperty("access_token").GetString()!;

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(idToken);
        var atHash = jwt.Claims.FirstOrDefault(c => c.Type == "at_hash")?.Value;
        Assert.False(string.IsNullOrEmpty(atHash));

        // at_hash = base64url(left-most half of SHA-256(access_token)) for an RS256 id_token.
        var fullHash = SHA256.HashData(Encoding.ASCII.GetBytes(accessToken));
        var expected = Base64Url(fullHash[..(fullHash.Length / 2)]);
        Assert.Equal(expected, atHash);
    }

    [Fact]
    public async Task IdentityToken_WithAlgNone_IsRejected()
    {
        var tokens = await GetTokensAsync("openid api");
        var idToken = tokens.GetProperty("id_token").GetString()!;

        // Strip the signature and force the header algorithm to "none" (classic algorithm-confusion
        // / signature-stripping attack). A correct validator must reject such a token.
        var parts = idToken.Split('.');
        var header = "{\"alg\":\"none\",\"typ\":\"JWT\"}";
        var unsigned = Base64Url(Encoding.UTF8.GetBytes(header)) + "." + parts[1] + ".";

        var jwks = await GetJwksAsync();
        var parameters = new TokenValidationParameters
        {
            ValidIssuer = OidcTestPipeline.BaseUrl,
            ValidAudience = ClientId,
            IssuerSigningKeys = jwks.GetSigningKeys(),
            ValidateLifetime = false
        };

        var handler = new JwtSecurityTokenHandler();
        Assert.ThrowsAny<SecurityTokenException>(() =>
            handler.ValidateToken(unsigned, parameters, out _));
    }

    [Fact]
    public async Task RefreshToken_IsRotated_AndOldTokenCannotBeReused()
    {
        var tokens = await GetTokensAsync();
        var originalRefreshToken = tokens.GetProperty("refresh_token").GetString()!;

        // First refresh succeeds and (because RefreshTokenUsage defaults to OneTimeOnly) must hand
        // out a NEW refresh token, i.e. the token is rotated.
        var firstResponse = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["refresh_token"] = originalRefreshToken
            }));

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        using var firstDoc = JsonDocument.Parse(await firstResponse.Content.ReadAsStringAsync());
        var rotatedRefreshToken = firstDoc.RootElement.GetProperty("refresh_token").GetString()!;
        Assert.NotEqual(originalRefreshToken, rotatedRefreshToken);

        // Reusing the ORIGINAL (now consumed) refresh token must be rejected: this is what allows a
        // server to detect refresh-token theft/replay.
        var reuseResponse = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["refresh_token"] = originalRefreshToken
            }));

        Assert.Equal(HttpStatusCode.BadRequest, reuseResponse.StatusCode);
        using var reuseDoc = JsonDocument.Parse(await reuseResponse.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", reuseDoc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AccessToken_ExpiresAfterConfiguredLifetime()
    {
        // The clock is frozen, so the token's exp must be exactly issued-at + the configured
        // lifetime, and expires_in must report that same lifetime.
        var issuedAt = _pipeline.Clock.GetUtcNow();

        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ShortLivedClientId,
                ["client_secret"] = ShortLivedClientSecret,
                ["scope"] = "api"
            }));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(ShortLifetimeSeconds, doc.RootElement.GetProperty("expires_in").GetInt32());

        // The reference token is opaque, so introspect it to read the exp claim.
        var token = doc.RootElement.GetProperty("access_token").GetString()!;
        var introspection = await IntrospectAsync(token);
        Assert.True(introspection.GetProperty("active").GetBoolean());

        var exp = introspection.GetProperty("exp").GetInt64();
        Assert.Equal(issuedAt.ToUnixTimeSeconds() + ShortLifetimeSeconds, exp);
    }

    [Fact]
    public async Task ReferenceToken_IsRejectedAfterItExpires()
    {
        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ShortLivedClientId,
                ["client_secret"] = ShortLivedClientSecret,
                ["scope"] = "api"
            }));

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var token = doc.RootElement.GetProperty("access_token").GetString()!;

        // Immediately after issuance the token is active.
        Assert.True((await IntrospectAsync(token)).GetProperty("active").GetBoolean());

        // Advance the clock just past the token's lifetime; the now-expired token must be rejected.
        _pipeline.Clock.Advance(TimeSpan.FromSeconds(ShortLifetimeSeconds + 1));

        Assert.False((await IntrospectAsync(token)).GetProperty("active").GetBoolean());
    }

    /// <summary>Introspects an access token using the API resource's credentials.</summary>
    private async Task<JsonElement> IntrospectAsync(string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, OidcTestPipeline.IntrospectionEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token })
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"api:{ApiSecret}"));
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", basic);

        var response = await _pipeline.BackChannelClient.SendAsync(request);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }
}
