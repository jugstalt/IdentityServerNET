using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using IdentityServer4.Models;
using Xunit;

namespace IdentityServerNET.Tests.Oidc;

/// <summary>
/// Verifies the OpenID Connect discovery document and JWKS endpoint. The discovery document is
/// the contract every relying party relies on, so these tests pin that the expected protocol
/// endpoints and signing keys are advertised and that no private key material is leaked.
/// </summary>
public class DiscoveryEndpointTests
{
    private readonly OidcTestPipeline _pipeline = new();

    public DiscoveryEndpointTests()
    {
        _pipeline.IdentityScopes.Add(new IdentityResources.OpenId());
        _pipeline.ApiScopes.Add(new ApiScope("api"));
        _pipeline.Initialize();
    }

    [Fact]
    public async Task Discovery_Document_Advertises_Core_Endpoints()
    {
        var response = await _pipeline.BackChannelClient.GetAsync(OidcTestPipeline.DiscoveryEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.Equal(OidcTestPipeline.BaseUrl, root.GetProperty("issuer").GetString());
        Assert.Equal(OidcTestPipeline.AuthorizeEndpoint, root.GetProperty("authorization_endpoint").GetString());
        Assert.Equal(OidcTestPipeline.TokenEndpoint, root.GetProperty("token_endpoint").GetString());
        Assert.Equal(OidcTestPipeline.UserInfoEndpoint, root.GetProperty("userinfo_endpoint").GetString());
        Assert.Equal(OidcTestPipeline.DiscoveryKeysEndpoint, root.GetProperty("jwks_uri").GetString());
    }

    [Fact]
    public async Task Discovery_Document_Advertises_Asymmetric_Signing_Algorithm()
    {
        var response = await _pipeline.BackChannelClient.GetAsync(OidcTestPipeline.DiscoveryEndpoint);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        var algs = doc.RootElement
            .GetProperty("id_token_signing_alg_values_supported")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToList();

        // The developer signing credential is RSA, so RS256 must be offered and no
        // "none"/symmetric algorithm may be advertised for id_token signing.
        Assert.Contains("RS256", algs);
        Assert.DoesNotContain("none", algs);
        Assert.DoesNotContain("HS256", algs);
    }

    [Fact]
    public async Task Jwks_Exposes_Public_Key_Without_Private_Material()
    {
        var response = await _pipeline.BackChannelClient.GetAsync(OidcTestPipeline.DiscoveryKeysEndpoint);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var keys = doc.RootElement.GetProperty("keys");

        Assert.True(keys.GetArrayLength() >= 1, "At least one signing key must be published.");

        var key = keys[0];
        Assert.Equal("RSA", key.GetProperty("kty").GetString());
        Assert.Equal("RS256", key.GetProperty("alg").GetString());
        // Public modulus/exponent must be present...
        Assert.False(string.IsNullOrEmpty(key.GetProperty("n").GetString()));
        Assert.False(string.IsNullOrEmpty(key.GetProperty("e").GetString()));

        // ...but no PRIVATE RSA components (d, p, q, dp, dq, qi) may ever be exposed.
        foreach (var privateComponent in new[] { "d", "p", "q", "dp", "dq", "qi" })
        {
            Assert.False(key.TryGetProperty(privateComponent, out _),
                $"JWKS leaked private RSA component '{privateComponent}'.");
        }
    }
}
