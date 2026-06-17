using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Tests the OAuth2 client credentials grant (machine-to-machine). Covers the happy path plus
/// the most important negative/security cases: wrong secret, requesting a scope the client is
/// not allowed to use, and an unknown client.
/// </summary>
public class ClientCredentialsFlowTests
{
    private const string ClientId = "m2m";
    private const string ClientSecret = "m2m-secret";
    private const string AllowedScope = "api.read";
    private const string ForbiddenScope = "api.write";

    private readonly OidcTestPipeline _pipeline = new();

    public ClientCredentialsFlowTests()
    {
        _pipeline.Clients.Add(new Client
        {
            ClientId = ClientId,
            ClientSecrets = { new Secret(ClientSecret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { AllowedScope }
        });

        _pipeline.ApiScopes.Add(new ApiScope(AllowedScope));
        _pipeline.ApiScopes.Add(new ApiScope(ForbiddenScope));

        _pipeline.Initialize();
    }

    private Task<HttpResponseMessage> RequestTokenAsync(IEnumerable<KeyValuePair<string, string>> form)
        => _pipeline.BackChannelClient.PostAsync(OidcTestPipeline.TokenEndpoint, new FormUrlEncodedContent(form));

    [Fact]
    public async Task ValidClientAndSecret_ReturnsAccessToken()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = AllowedScope
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.False(root.TryGetProperty("error", out _));
        Assert.False(string.IsNullOrEmpty(root.GetProperty("access_token").GetString()));
        Assert.Equal("Bearer", root.GetProperty("token_type").GetString());
        Assert.True(root.GetProperty("expires_in").GetInt32() > 0);
    }

    [Fact]
    public async Task WrongSecret_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ClientId,
            ["client_secret"] = "not-the-secret",
            ["scope"] = AllowedScope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_client", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task RequestingForbiddenScope_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = ForbiddenScope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_scope", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UnknownClient_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = "does-not-exist",
            ["client_secret"] = "whatever",
            ["scope"] = AllowedScope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("invalid_client", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task UnsupportedGrantType_IsRejected()
    {
        var response = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "not-a-real-grant",
            ["client_id"] = ClientId,
            ["client_secret"] = ClientSecret,
            ["scope"] = AllowedScope
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("unsupported_grant_type", doc.RootElement.GetProperty("error").GetString());
    }
}
