using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Microsoft.AspNetCore.WebUtilities;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Tests the OpenID Connect authorization code flow with PKCE, the recommended flow for
/// interactive clients. Exercises the full round-trip (login -> authorize -> code -> token) and
/// pins the critical security checks: PKCE verifier binding, redirect-uri validation and the
/// requirement to send a code challenge.
/// </summary>
public class AuthorizationCodePkceFlowTests
{
    private const string ClientId = "code-client";
    private const string ClientSecret = "code-secret";
    private const string RedirectUri = "https://client/callback";
    private const string SubjectId = "alice";

    private readonly OidcTestPipeline _pipeline = new();

    public AuthorizationCodePkceFlowTests()
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
            Password = "Pass123$"
        });

        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.IdentityScopes.Add(new IdentityResources.Profile());
        _pipeline.ApiScopes.Add(new ApiScope("api"));

        _pipeline.Initialize();
    }

    private static string CreateCodeVerifier() => Base64Url(RandomNumberGenerator.GetBytes(32));

    private static string CreateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64Url(hash);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static string BuildAuthorizeUrl(
        string scope,
        string redirectUri,
        string state,
        string nonce,
        string? codeChallenge)
    {
        var url = $"{OidcTestPipeline.AuthorizeEndpoint}" +
                  $"?client_id={ClientId}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString(scope)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={state}" +
                  $"&nonce={nonce}";

        if (codeChallenge != null)
        {
            url += $"&code_challenge={codeChallenge}&code_challenge_method=S256";
        }

        return url;
    }

    [Fact]
    public async Task FullCodeExchange_WithPkce_ReturnsTokens()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var challenge = CreateCodeChallenge(verifier);

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var authorizeResponse = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl("openid profile api offline_access", RedirectUri, "state123", "nonce123", challenge));

        Assert.Equal(HttpStatusCode.Found, authorizeResponse.StatusCode);
        var location = authorizeResponse.Headers.Location!;
        Assert.StartsWith(RedirectUri, location.GetLeftPart(UriPartial.Path));

        var query = QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("state123", query["state"]);
        var code = query["code"].ToString();
        Assert.False(string.IsNullOrEmpty(code));

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

        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);

        using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        Assert.False(root.TryGetProperty("error", out _));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("access_token").GetString()));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("id_token").GetString()));
        // offline_access was requested -> a refresh token must be issued.
        Assert.False(string.IsNullOrEmpty(root.GetProperty("refresh_token").GetString()));
    }

    [Fact]
    public async Task CodeExchange_WithWrongPkceVerifier_IsRejected()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var challenge = CreateCodeChallenge(verifier);

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var authorizeResponse = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl("openid api", RedirectUri, "state123", "nonce123", challenge));

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
                ["code_verifier"] = CreateCodeVerifier() // different verifier -> PKCE mismatch
            }));

        Assert.Equal(HttpStatusCode.BadRequest, tokenResponse.StatusCode);
        using var doc = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Authorize_WithUnregisteredRedirectUri_DoesNotRedirectToClient()
    {
        await _pipeline.LoginAsync(SubjectId);

        var challenge = CreateCodeChallenge(CreateCodeVerifier());

        _pipeline.BrowserClient.AllowAutoRedirect = true;
        var response = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl("openid", "https://evil/callback", "state123", "nonce123", challenge));

        // IdentityServer must never redirect back to an unregistered (untrusted) redirect_uri;
        // instead the request lands on the error page handled by the pipeline.
        Assert.True(_pipeline.ErrorWasCalled, "Invalid redirect_uri should surface on the error page.");
        Assert.NotNull(_pipeline.ErrorMessage);
    }

    [Fact]
    public async Task Authorize_WithoutCodeChallenge_IsRejectedForPkceClient()
    {
        await _pipeline.LoginAsync(SubjectId);

        _pipeline.BrowserClient.AllowAutoRedirect = true;
        await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl("openid", RedirectUri, "state123", "nonce123", codeChallenge: null));

        // "code challenge required" maps to the invalid_request error, which IdentityServer does
        // NOT consider a "safe" error: it must not be redirected back to the client. Instead the
        // request lands on the error page, so no authorization code is ever exposed.
        Assert.True(_pipeline.ErrorWasCalled, "Missing code_challenge should surface on the error page.");
        Assert.NotNull(_pipeline.ErrorMessage);
        Assert.Equal("invalid_request", _pipeline.ErrorMessage!.Error);
    }
}
