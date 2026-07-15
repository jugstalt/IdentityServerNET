using System.Collections.Generic;
using IdentityServerNET.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// <see cref="ServiceCollectionExtensions.ConfigureIdentityServerApplicationCookie"/> must set
/// HttpOnly/SameSite explicitly rather than relying on ASP.NET Core Identity's current implicit
/// defaults (which happen to match today, but must not be able to silently weaken under a future
/// framework default change) - and this must hold regardless of whether any IdentityServer:Cookie:*
/// / PublicOrigin config is present.
/// </summary>
public class ConfigureIdentityServerApplicationCookieTests
{
    private static CookieAuthenticationOptions Configure(Dictionary<string, string?> configData)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureIdentityServerApplicationCookie(configuration);

        using var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);
    }

    [Fact]
    public void HttpOnly_And_SameSite_Are_Explicit_With_No_Config_At_All()
    {
        var options = Configure(new Dictionary<string, string?>());

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
    }

    [Fact]
    public void HttpOnly_And_SameSite_Are_Explicit_When_PublicOrigin_Is_Https()
    {
        var options = Configure(new Dictionary<string, string?>
        {
            ["IdentityServer:PublicOrigin"] = "https://identity.example.com"
        });

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.SameAsRequest, options.Cookie.SecurePolicy);
    }

    [Fact]
    public void SecurePolicy_Is_Forced_To_Always_When_PublicOrigin_Is_Http()
    {
        var options = Configure(new Dictionary<string, string?>
        {
            ["IdentityServer:PublicOrigin"] = "http://localhost:5000"
        });

        Assert.True(options.Cookie.HttpOnly);
        Assert.Equal(SameSiteMode.Lax, options.Cookie.SameSite);
        Assert.Equal(CookieSecurePolicy.Always, options.Cookie.SecurePolicy);
    }
}
