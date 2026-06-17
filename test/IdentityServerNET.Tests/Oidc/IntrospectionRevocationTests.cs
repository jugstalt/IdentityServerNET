using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Tests token introspection (RFC 7662) and revocation (RFC 7009). Introspection lets a protected
/// API check whether an access token is active; revocation lets a client invalidate a token. The
/// tests pin that a valid token is reported active, that a revoked token is reported inactive, and
/// that both endpoints require proper authentication.
/// </summary>
public class IntrospectionRevocationTests
{
    private const string ClientId = "m2m";
    private const string ClientSecret = "m2m-secret";
    private const string ApiName = "api";
    private const string ApiSecret = "api-secret";
    private const string Scope = "api";

    private readonly OidcTestPipeline _pipeline = new();

    public IntrospectionRevocationTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = ClientId,
            ClientSecrets = { new Secret(ClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { Scope }
        });

        _pipeline.ApiResources.Add(new ApiResource(ApiName)
        {
            ApiSecrets = { new Secret(ApiSecret.Sha256()) },
            Scopes = { Scope }
        });

        _pipeline.ApiScopes.Add(new ApiScope(Scope));

        _pipeline.Initialize();
    }

    private async Task<string> GetAccessTokenAsync()
    {
        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.TokenEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = ClientId,
                ["client_secret"] = ClientSecret,
                ["scope"] = Scope
            }));

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("access_token").GetString()!;
    }

    private HttpRequestMessage CreateBasicAuthRequest(string endpoint, string id, string secret,
        IEnumerable<KeyValuePair<string, string>> form)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{id}:{secret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);
        return request;
    }

    [Fact]
    public async Task Introspection_WithValidToken_ReportsActive()
    {
        var token = await GetAccessTokenAsync();

        var request = CreateBasicAuthRequest(
            OidcTestPipeline.IntrospectionEndpoint, ApiName, ApiSecret,
            new Dictionary<string, string> { ["token"] = token });
        var response = await _pipeline.BackChannelClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Introspection_WithInvalidToken_ReportsInactive()
    {
        var request = CreateBasicAuthRequest(
            OidcTestPipeline.IntrospectionEndpoint, ApiName, ApiSecret,
            new Dictionary<string, string> { ["token"] = "this-is-not-a-valid-token" });
        var response = await _pipeline.BackChannelClient.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(doc.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Introspection_WithoutApiSecret_IsUnauthorized()
    {
        var token = await GetAccessTokenAsync();

        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.IntrospectionEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token }));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RevokedReferenceToken_IsReportedInactive()
    {
        // Reference tokens can be revoked server-side; switch the client to reference tokens.
        _pipeline.Clients[0].AccessTokenType = AccessTokenType.Reference;

        var token = await GetAccessTokenAsync();

        // Sanity check: active before revocation.
        var before = await _pipeline.BackChannelClient.SendAsync(CreateBasicAuthRequest(
            OidcTestPipeline.IntrospectionEndpoint, ApiName, ApiSecret,
            new Dictionary<string, string> { ["token"] = token }));
        using (var beforeDoc = JsonDocument.Parse(await before.Content.ReadAsStringAsync()))
        {
            Assert.True(beforeDoc.RootElement.GetProperty("active").GetBoolean());
        }

        // Revoke it using the client's credentials.
        var revoke = await _pipeline.BackChannelClient.SendAsync(CreateBasicAuthRequest(
            OidcTestPipeline.RevocationEndpoint, ClientId, ClientSecret,
            new Dictionary<string, string>
            {
                ["token"] = token,
                ["token_type_hint"] = "access_token"
            }));
        Assert.Equal(HttpStatusCode.OK, revoke.StatusCode);

        // Now it must be inactive.
        var after = await _pipeline.BackChannelClient.SendAsync(CreateBasicAuthRequest(
            OidcTestPipeline.IntrospectionEndpoint, ApiName, ApiSecret,
            new Dictionary<string, string> { ["token"] = token }));
        using var afterDoc = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.False(afterDoc.RootElement.GetProperty("active").GetBoolean());
    }

    [Fact]
    public async Task Revocation_WithoutClientAuthentication_IsRejected()
    {
        var token = await GetAccessTokenAsync();

        var response = await _pipeline.BackChannelClient.PostAsync(
            OidcTestPipeline.RevocationEndpoint,
            new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
