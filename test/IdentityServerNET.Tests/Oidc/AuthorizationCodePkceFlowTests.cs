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
    // A second confidential code client sharing the same redirect_uri. Used to prove that an
    // authorization code issued to one client cannot be redeemed by a different client.
    private const string OtherClientId = "code-client-2";
    private const string OtherClientSecret = "code-secret-2";
    // A code client that requires consent. Used to exercise the consent-denied path.
    private const string ConsentClientId = "code-client-consent";
    private const string ConsentClientSecret = "code-consent-secret";
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

        _pipeline.Clients.Add(new Client
        {
            ClientId = OtherClientId,
            ClientSecrets = { new Secret(OtherClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireConsent = false,
            RedirectUris = { RedirectUri },
            AllowedScopes = { "openid", "profile", "api" },
            AllowOfflineAccess = true
        });

        _pipeline.Clients.Add(new Client
        {
            ClientId = ConsentClientId,
            ClientSecrets = { new Secret(ConsentClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.Code,
            RequirePkce = true,
            RequireConsent = true,
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

    /// <summary>
    /// Flexible authorize-URL builder used by the security tests. Allows overriding the client,
    /// the PKCE challenge method and adding a <c>prompt</c> value.
    /// </summary>
    private static string BuildAuthorizeUrl(
        string clientId,
        string scope,
        string redirectUri,
        string state,
        string nonce,
        string? codeChallenge,
        string codeChallengeMethod = "S256",
        string? prompt = null)
    {
        var url = $"{OidcTestPipeline.AuthorizeEndpoint}" +
                  $"?client_id={clientId}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString(scope)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={state}" +
                  $"&nonce={nonce}";

        if (codeChallenge != null)
        {
            url += $"&code_challenge={codeChallenge}&code_challenge_method={codeChallengeMethod}";
        }

        if (prompt != null)
        {
            url += $"&prompt={prompt}";
        }

        return url;
    }

    /// <summary>
    /// Drives login + authorize for the given client and returns the issued authorization code.
    /// </summary>
    private async Task<string> GetAuthorizationCodeAsync(string clientId, string challenge, string scope = "openid api")
    {
        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var authorizeResponse = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl(clientId, scope, RedirectUri, "state123", "nonce123", challenge));

        return QueryHelpers.ParseQuery(authorizeResponse.Headers.Location!.Query)["code"].ToString();
    }

    /// <summary>POSTs an authorization_code token request and returns the raw response.</summary>
    private Task<HttpResponseMessage> ExchangeCodeAsync(
        string code,
        string verifier,
        string clientId = ClientId,
        string clientSecret = ClientSecret,
        string redirectUri = RedirectUri)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = verifier
        };

        return _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint, new FormUrlEncodedContent(form));
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

    [Fact]
    public async Task AuthorizationCode_CanOnlyBeRedeemedOnce()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var code = await GetAuthorizationCodeAsync(ClientId, CreateCodeChallenge(verifier));

        // First exchange succeeds.
        var first = await ExchangeCodeAsync(code, verifier);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        // Replaying the same code must be rejected (RFC 6749 §4.1.2: a code is single-use).
        var second = await ExchangeCodeAsync(code, verifier);
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);

        using var doc = JsonDocument.Parse(await second.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task AuthorizationCode_IssuedToOneClient_CannotBeRedeemedByAnother()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        // Code is issued to "code-client"...
        var code = await GetAuthorizationCodeAsync(ClientId, CreateCodeChallenge(verifier));

        // ...but redeemed with the (valid) credentials of "code-client-2". The code is bound to the
        // requesting client, so this must fail and prevents code injection across clients.
        var response = await ExchangeCodeAsync(code, verifier, OtherClientId, OtherClientSecret);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CodeExchange_WithMismatchedRedirectUri_IsRejected()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var code = await GetAuthorizationCodeAsync(ClientId, CreateCodeChallenge(verifier));

        // The redirect_uri at the token endpoint must exactly match the one used at /authorize.
        var response = await ExchangeCodeAsync(code, verifier, redirectUri: "https://client/other-callback");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task CodeExchange_WithoutClientSecret_IsRejected()
    {
        await _pipeline.LoginAsync(SubjectId);

        var verifier = CreateCodeVerifier();
        var code = await GetAuthorizationCodeAsync(ClientId, CreateCodeChallenge(verifier));

        // A confidential client must authenticate at the token endpoint; PKCE does not replace
        // client authentication. Omitting the secret must fail with invalid_client.
        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = ClientId,
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = verifier
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_client", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Authorize_WithPlainPkceMethod_IsRejected()
    {
        await _pipeline.LoginAsync(SubjectId);

        // The client does not allow plain-text PKCE, so the weaker "plain" method must be rejected
        // (downgrade protection): only S256 is acceptable.
        _pipeline.BrowserClient.AllowAutoRedirect = true;
        await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl(ClientId, "openid", RedirectUri, "state123", "nonce123",
                CreateCodeVerifier(), codeChallengeMethod: "plain"));

        Assert.True(_pipeline.ErrorWasCalled, "plain PKCE method should surface on the error page.");
        Assert.NotNull(_pipeline.ErrorMessage);
        Assert.Equal("invalid_request", _pipeline.ErrorMessage!.Error);
    }

    [Fact]
    public async Task Authorize_EchoesStateBackToClient()
    {
        await _pipeline.LoginAsync(SubjectId);

        const string state = "xyz-csrf-state";
        var challenge = CreateCodeChallenge(CreateCodeVerifier());

        _pipeline.BrowserClient.AllowAutoRedirect = false;
        var response = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl(ClientId, "openid", RedirectUri, state, "nonce123", challenge));

        // The state parameter must be returned verbatim so the client can defend against CSRF.
        var query = QueryHelpers.ParseQuery(response.Headers.Location!.Query);
        Assert.Equal(state, query["state"]);
    }

    [Fact]
    public async Task Authorize_WithPromptNone_AndNoSession_ReturnsLoginRequired()
    {
        // No LoginAsync -> there is no authenticated session. With prompt=none the server must not
        // show any UI; instead it returns the login_required error back to the client.
        var challenge = CreateCodeChallenge(CreateCodeVerifier());

        _pipeline.BrowserClient.AllowAutoRedirect = true;
        var response = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl(ClientId, "openid", RedirectUri, "state123", "nonce123", challenge, prompt: "none"));

        // login_required is a "safe" error, so it is redirected to the (registered) client callback,
        // which has no route on the test host and therefore yields a 404 on the final hop.
        var finalUri = response.RequestMessage!.RequestUri!;
        Assert.StartsWith(RedirectUri, finalUri.GetLeftPart(UriPartial.Path));
        Assert.Equal("login_required", QueryHelpers.ParseQuery(finalUri.Query)["error"]);
        Assert.False(_pipeline.LoginWasCalled, "prompt=none must not trigger the login UI.");
    }

    [Fact]
    public async Task Authorize_WhenConsentIsDenied_ReturnsAccessDenied()
    {
        await _pipeline.LoginAsync(SubjectId);

        // The user is asked for consent (RequireConsent=true) and denies it.
        _pipeline.ConsentResponse = new ConsentResponse { Error = AuthorizationError.AccessDenied };

        var challenge = CreateCodeChallenge(CreateCodeVerifier());

        _pipeline.BrowserClient.AllowAutoRedirect = true;
        var response = await _pipeline.BrowserClient.GetAsync(
            BuildAuthorizeUrl(ConsentClientId, "openid api", RedirectUri, "state123", "nonce123", challenge));

        Assert.True(_pipeline.ConsentWasCalled, "Consent page should have been reached.");

        // access_denied is a "safe" error and must be returned to the client (no code is issued).
        var finalUri = response.RequestMessage!.RequestUri!;
        Assert.StartsWith(RedirectUri, finalUri.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(finalUri.Query);
        Assert.Equal("access_denied", query["error"]);
        Assert.False(query.ContainsKey("code"), "No authorization code must be issued when consent is denied.");
    }
}
