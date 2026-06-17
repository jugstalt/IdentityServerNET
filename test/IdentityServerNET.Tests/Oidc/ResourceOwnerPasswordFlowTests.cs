using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using IdentityServer4.Test;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Tests the OAuth2 resource owner password credentials (ROPC) grant.
/// <para>
/// SECURITY NOTE: ROPC is deprecated by the OAuth 2.0 Security BCP and removed from OAuth 2.1
/// because it exposes the user's credentials directly to the client and is incompatible with MFA,
/// federation and modern phishing-resistant authentication. New applications should use the
/// authorization code flow with PKCE instead. These tests exist only because IdentityServer still
/// supports the grant for legacy clients; they pin the happy path and the security-relevant
/// negatives: wrong password, inactive user, and a client that is not allowed to use the grant.
/// </para>
/// </summary>
public class ResourceOwnerPasswordFlowTests
{
    private const string RopcClientId = "ropc";
    private const string ClientCredentialsOnlyClientId = "cc-only";
    private const string ClientSecret = "ropc-secret";
    private const string Scope = "api";

    private const string Username = "alice";
    private const string Password = "Pass123$";

    private readonly OidcTestPipeline _pipeline = new();

    public ResourceOwnerPasswordFlowTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = RopcClientId,
            ClientSecrets = { new Secret(ClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ResourceOwnerPassword,
            AllowedScopes = { Scope }
        });

        // A client that explicitly does NOT permit the password grant.
        _pipeline.Clients.Add(new Client
        {
            ClientId = ClientCredentialsOnlyClientId,
            ClientSecrets = { new Secret(ClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scope }
        });

        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "1",
            Username = Username,
            Password = Password,
            Claims = new[] { new Claim("name", "Alice Smith") }
        });

        _pipeline.Users.Add(new TestUser
        {
            SubjectId = "2",
            Username = "inactive",
            Password = Password,
            IsActive = false
        });

        _pipeline.ApiScopes.Add(new ApiScope(Scope));

        _pipeline.Initialize();
    }

    private Task<HttpResponseMessage> RequestTokenAsync(IEnumerable<KeyValuePair<string, string>> form)
        => _pipeline.BackChannelClient.PostAsync(OidcTestPipeline.TokenEndpoint, new FormUrlEncodedContent(form));

    [Fact]
    public async Task ValidUserCredentials_ReturnAccessToken()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = ClientSecret,
            ["username"] = Username,
            ["password"] = Password,
            ["scope"] = Scope
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.TryGetProperty("error", out _));
        Assert.False(string.IsNullOrEmpty(doc.RootElement.GetProperty("access_token").GetString()));
    }

    [Fact]
    public async Task WrongPassword_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = ClientSecret,
            ["username"] = Username,
            ["password"] = "wrong-password",
            ["scope"] = Scope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InactiveUser_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = RopcClientId,
            ["client_secret"] = ClientSecret,
            ["username"] = "inactive",
            ["password"] = Password,
            ["scope"] = Scope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_grant", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task ClientWithoutPasswordGrant_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["client_id"] = ClientCredentialsOnlyClientId,
            ["client_secret"] = ClientSecret,
            ["username"] = Username,
            ["password"] = Password,
            ["scope"] = Scope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("unauthorized_client", doc.RootElement.GetProperty("error").GetString());
    }
}
