using IdentityServer.RateLimiting;
using Microsoft.AspNetCore.Http;
using System.Net;
using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Tests for <see cref="TokenEndpointRateLimiterFactory"/>: the interactive login page has its own
/// bot-detection/CAPTCHA, but the OAuth token endpoint (password grant, client_credentials) is a
/// separate attack surface that bypasses it entirely. These tests exercise the actual partitioning
/// decision directly, without booting a host - the token endpoint path must be limited per-IP, and
/// every other path must never be limited at all.
/// </summary>
public class TokenEndpointRateLimiterFactoryTests
{
    private static HttpContext FakeHttpContext(string path, string remoteIp)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        return context;
    }

    [Fact]
    public void TokenEndpoint_RejectsRequests_OnceThePermitLimitIsExceeded()
    {
        var limiter = TokenEndpointRateLimiterFactory.Create("/connect/token", permitLimit: 2, window: TimeSpan.FromMinutes(1));
        var context = FakeHttpContext("/connect/token", "203.0.113.10");

        using var lease1 = limiter.AttemptAcquire(context);
        using var lease2 = limiter.AttemptAcquire(context);
        using var lease3 = limiter.AttemptAcquire(context);

        Assert.True(lease1.IsAcquired);
        Assert.True(lease2.IsAcquired);
        Assert.False(lease3.IsAcquired, "A third request within the window must be rejected.");
    }

    [Fact]
    public void OtherPaths_AreNeverRateLimited()
    {
        var limiter = TokenEndpointRateLimiterFactory.Create("/connect/token", permitLimit: 1, window: TimeSpan.FromMinutes(1));
        var context = FakeHttpContext("/connect/authorize", "203.0.113.10");

        for (var i = 0; i < 20; i++)
        {
            using var lease = limiter.AttemptAcquire(context);
            Assert.True(lease.IsAcquired, $"Request #{i + 1} to an unrelated path must never be rate limited.");
        }
    }

    [Fact]
    public void DifferentRemoteIps_AreTrackedAsIndependentPartitions()
    {
        var limiter = TokenEndpointRateLimiterFactory.Create("/connect/token", permitLimit: 1, window: TimeSpan.FromMinutes(1));
        var contextA = FakeHttpContext("/connect/token", "203.0.113.10");
        var contextB = FakeHttpContext("/connect/token", "203.0.113.20");

        using var leaseA1 = limiter.AttemptAcquire(contextA);
        using var leaseB1 = limiter.AttemptAcquire(contextB);
        using var leaseA2 = limiter.AttemptAcquire(contextA);

        Assert.True(leaseA1.IsAcquired);
        Assert.True(leaseB1.IsAcquired, "A different IP must have its own, independent budget.");
        Assert.False(leaseA2.IsAcquired, "The first IP's budget must already be exhausted.");
    }
}
