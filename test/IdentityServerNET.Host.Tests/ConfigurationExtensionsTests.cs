using IdentityServerNET.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Tests for <see cref="ConfigurationExtensions.PublicOriginIsHttp"/>, which decides whether the
/// self-referencing JWT bearer metadata fetch (Bearer-Secrets/Bearer-Signing) may relax
/// RequireHttpsMetadata. It must only relax for a genuinely HTTP PublicOrigin - never unconditionally,
/// which was the previous (overly broad) behavior.
/// </summary>
public class ConfigurationExtensionsTests
{
    private static IConfiguration ConfigWithPublicOrigin(string? publicOrigin)
    {
        var data = new Dictionary<string, string?>();
        if (publicOrigin is not null)
        {
            data["IdentityServer:PublicOrigin"] = publicOrigin;
        }

        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void ReturnsTrue_ForHttpPublicOrigin()
    {
        var config = ConfigWithPublicOrigin("http://localhost:5000");

        Assert.True(config.PublicOriginIsHttp());
    }

    [Fact]
    public void ReturnsFalse_ForHttpsPublicOrigin()
    {
        var config = ConfigWithPublicOrigin("https://identity.example.com");

        Assert.False(config.PublicOriginIsHttp());
    }

    [Fact]
    public void ReturnsFalse_WhenPublicOriginIsNotConfigured()
    {
        var config = ConfigWithPublicOrigin(null);

        Assert.False(config.PublicOriginIsHttp());
    }

    [Fact]
    public void ReturnsFalse_ForMalformedPublicOrigin()
    {
        var config = ConfigWithPublicOrigin("not-a-valid-uri");

        Assert.False(config.PublicOriginIsHttp());
    }
}
