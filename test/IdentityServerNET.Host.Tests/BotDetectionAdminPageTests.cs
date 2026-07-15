using IdentityServer.Areas.Admin.Pages.BotDetection;
using IdentityServerNET.Abstractions.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Tests for the "Bot Detection" admin page (<c>/Admin/BotDetection</c>) against the real
/// <see cref="ILoginBotDetection"/> registered in the host's DI container (same instance
/// <c>AccountController</c>/<c>LoginWith2fa</c>/<c>LoginWithRecoveryCode</c> use) - so a mismatch
/// between how the login flow tracks an entry and how this page reads/clears it would be caught.
///
/// Every test below uses a GUID-suffixed username/IP: the underlying <c>IDistributedCache</c> is
/// shared for the whole test collection (see <see cref="HostApplicationFactory"/>), and IP suspicion
/// in particular is never auto-cleared, so reusing a fixed value here would leak state into other
/// tests in the same run.
/// </summary>
[Collection(HostTestCollection.Name)]
public class BotDetectionAdminPageTests
{
    private readonly HostApplicationFactory _factory;

    public BotDetectionAdminPageTests(HostApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task OnGetAsync_ListsSuspiciousUsersAndIps_FromTheSharedService()
    {
        using var scope = _factory.Services.CreateScope();
        var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();

        var username = $"botdetection-admin-{Guid.NewGuid():N}@example.com";
        var ip = $"203.0.113.{Random.Shared.Next(1, 254)}";

        await botDetection.AddSuspiciousUserAsync(username);
        await botDetection.AddSuspiciousIpAsync(ip);

        var page = new IndexModel(botDetection);
        await page.OnGetAsync();

        Assert.True(page.HasBotDetection);
        Assert.Contains(page.SuspiciousUsers, e => e.Key == username.ToUpperInvariant());
        Assert.Contains(page.SuspiciousIps, e => e.Key == ip);
    }

    [Fact]
    public async Task OnGetClearUserAsync_RemovesTheEntry_AndRedirectsBackToTheList()
    {
        using var scope = _factory.Services.CreateScope();
        var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();

        var username = $"botdetection-admin-clear-{Guid.NewGuid():N}@example.com";
        await botDetection.AddSuspiciousUserAsync(username);

        var page = new IndexModel(botDetection);
        var result = await page.OnGetClearUserAsync(username);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(await botDetection.IsSuspiciousUserAsync(username));
        Assert.DoesNotContain(await botDetection.GetSuspiciousUsersAsync(), e => e.Key == username.ToUpperInvariant());
    }

    [Fact]
    public async Task OnGetClearIpAsync_RemovesTheEntry_EvenThoughTheLoginFlowNeverWould()
    {
        using var scope = _factory.Services.CreateScope();
        var botDetection = scope.ServiceProvider.GetRequiredService<ILoginBotDetection>();

        var ip = $"203.0.113.{Random.Shared.Next(1, 254)}";
        await botDetection.AddSuspiciousIpAsync(ip);

        var page = new IndexModel(botDetection);
        var result = await page.OnGetClearIpAsync(ip);

        Assert.IsType<RedirectToPageResult>(result);
        Assert.False(await botDetection.IsSuspiciousIpAsync(ip));
        Assert.DoesNotContain(await botDetection.GetSuspiciousIpsAsync(), e => e.Key == ip);
    }

    [Fact]
    public async Task WithoutLoginBotDetectionRegistered_PageDegradesGracefully()
    {
        var page = new IndexModel(loginBotDetection: null);

        await page.OnGetAsync();
        Assert.False(page.HasBotDetection);
        Assert.Empty(page.SuspiciousUsers);
        Assert.Empty(page.SuspiciousIps);

        var result = await page.OnGetClearUserAsync("someone@example.com");
        Assert.IsType<RedirectToPageResult>(result);
        Assert.StartsWith("Error", page.StatusMessage);
    }
}
