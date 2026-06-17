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
}
