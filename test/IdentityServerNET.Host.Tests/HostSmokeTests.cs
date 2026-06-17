using System.Net;
using System.Text.Json;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Approach B: end-to-end smoke tests that boot the <em>real</em> IdentityServer host
/// (<see cref="Program"/>) through <see cref="HostApplicationFactory"/> and verify that the
/// fully-wired application starts and serves its core OpenID Connect endpoints.
///
/// These complement the lean, fully in-memory protocol tests (Approach A, in
/// IdentityServerNET.Tests) by exercising the actual production startup pipeline: Serilog,
/// the Aspire service defaults, ASP.NET Identity, the configuration/fallback service wiring
/// and the file-system signing-credential store (including on-boot certificate generation).
///
/// Because they boot the production host, these tests are deliberately kept in a separate
/// project: the host depends on Microsoft.Windows.Compatibility and writes to the file system,
/// so this suite is Windows-bound by design and is intentionally isolated from the
/// environment-independent Approach A suite.
/// </summary>
[Collection(HostTestCollection.Name)]
public class HostSmokeTests
{
    private readonly HostApplicationFactory _factory;

    public HostSmokeTests(HostApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Host_Boots_And_Serves_Discovery_Document()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("issuer", out _));
        Assert.True(root.TryGetProperty("authorization_endpoint", out _));
        Assert.True(root.TryGetProperty("token_endpoint", out _));
        Assert.True(root.TryGetProperty("jwks_uri", out _));
    }

    [Fact]
    public async Task Discovery_Advertises_A_Signing_Key()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/.well-known/openid-configuration/jwks");
        response.EnsureSuccessStatusCode();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var keys = doc.RootElement.GetProperty("keys");

        // The file-system signing-credential store must have produced at least one key on boot.
        Assert.True(keys.GetArrayLength() > 0, "The host must publish at least one signing key.");
    }

    [Fact]
    public async Task Health_Endpoint_Reports_Healthy()
    {
        var client = _factory.CreateClient();

        using var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
