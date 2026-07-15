using System;
using System.Threading.Tasks;
using IdentityServerNET.Exceptions;
using IdentityServerNET.Services.Security;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for <see cref="LoginBotDetection"/>, the brute-force / bot mitigation that
/// tracks failed logins, escalates to a CAPTCHA challenge and forgets stale entries.
/// </summary>
public class LoginBotDetectionTests
{
    private static LoginBotDetection CreateDetector(LoginBotDetectionOptions? options = null)
    {
        var cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        return new LoginBotDetection(
            cache,
            new TestOptionsMonitor<LoginBotDetectionOptions>(options ?? new LoginBotDetectionOptions()));
    }

    [Fact]
    public async Task IsSuspiciousUserAsync_ForUnknownUser_ReturnsFalse()
    {
        var detector = CreateDetector();

        Assert.False(await detector.IsSuspiciousUserAsync("unknown-user"));
    }

    [Fact]
    public async Task IsSuspiciousUserAsync_BecomesSuspicious_OnlyAfterReachingMaxFailCount()
    {
        var options = new LoginBotDetectionOptions { MaxFailCount = 3 };
        var detector = CreateDetector(options);
        const string user = "brute-force-target";

        await detector.AddSuspiciousUserAsync(user);
        await detector.AddSuspiciousUserAsync(user);
        Assert.False(await detector.IsSuspiciousUserAsync(user)); // 2 < 3

        await detector.AddSuspiciousUserAsync(user);
        Assert.True(await detector.IsSuspiciousUserAsync(user));  // 3 >= 3
    }

    [Fact]
    public async Task RemoveSuspiciousUserAsync_ResetsSuspicion()
    {
        var options = new LoginBotDetectionOptions { MaxFailCount = 1 };
        var detector = CreateDetector(options);
        const string user = "reset-me";

        await detector.AddSuspiciousUserAsync(user);
        Assert.True(await detector.IsSuspiciousUserAsync(user));

        await detector.RemoveSuspiciousUserAsync(user);
        Assert.False(await detector.IsSuspiciousUserAsync(user));
    }

    [Fact]
    public async Task IsSuspiciousUserAsync_ForgetsStaleEntries_AfterRememberWindow()
    {
        // RememberSuspiciousUserTotalMinutes = 0 -> the entry is considered stale immediately
        // and the user must no longer be treated as suspicious.
        var options = new LoginBotDetectionOptions
        {
            MaxFailCount = 1,
            RembemberSuspiciousUserTotalMinutes = 0
        };
        var detector = CreateDetector(options);
        const string user = "stale-user";

        await detector.AddSuspiciousUserAsync(user);

        Assert.False(await detector.IsSuspiciousUserAsync(user));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsSuspiciousUserAsync_WithEmptyUsername_Throws(string username)
    {
        var detector = CreateDetector();

        await Assert.ThrowsAsync<ArgumentException>(() => detector.IsSuspiciousUserAsync(username));
    }

    [Fact]
    public async Task AddSuspicousUserAndGenerateCaptchaCode_ProducesCodeOfConfiguredLengthFromAllowedAlphabet()
    {
        var options = new LoginBotDetectionOptions
        {
            CaptchaCodeLength = 6,
            CaptchaCodeLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        };
        var detector = CreateDetector(options);

        var code = await detector.AddSuspicousUserAndGenerateCaptchaCodeAsync("captcha-user");

        Assert.Equal(options.CaptchaCodeLength, code.Length);
        Assert.All(code, c => Assert.Contains(c, options.CaptchaCodeLetters));
    }

    [Fact]
    public async Task VerifyCaptchaCode_IsCaseInsensitive_ForCorrectCode()
    {
        var detector = CreateDetector();
        const string user = "captcha-verify-user";

        var code = await detector.AddSuspicousUserAndGenerateCaptchaCodeAsync(user);

        Assert.True(await detector.VerifyCaptchaCodeAsync(user, code));
        Assert.True(await detector.VerifyCaptchaCodeAsync(user, code.ToLowerInvariant()));
    }

    [Fact]
    public async Task VerifyCaptchaCode_WithWrongCode_ReturnsFalse()
    {
        var detector = CreateDetector();
        const string user = "captcha-wrong-user";

        await detector.AddSuspicousUserAndGenerateCaptchaCodeAsync(user);

        Assert.False(await detector.VerifyCaptchaCodeAsync(user, "definitely-wrong"));
    }

    [Fact]
    public async Task EnsureCaptchaCodeAsync_DoesNotCountAsAFailure()
    {
        // Regression test: redisplaying the password form on a plain GET must not itself push the
        // user toward (or past) MaxFailCount - only an actual failed sign-in attempt may do that.
        var options = new LoginBotDetectionOptions { MaxFailCount = 1 };
        var detector = CreateDetector(options);
        const string user = "get-redisplay-user";

        await detector.EnsureCaptchaCodeAsync(user);
        await detector.EnsureCaptchaCodeAsync(user);
        await detector.EnsureCaptchaCodeAsync(user);

        Assert.False(await detector.IsSuspiciousUserAsync(user));
    }

    [Fact]
    public async Task EnsureCaptchaCodeAsync_ProducesAVerifiableCode()
    {
        var detector = CreateDetector();
        const string user = "get-redisplay-verify-user";

        var code = await detector.EnsureCaptchaCodeAsync(user);

        Assert.True(await detector.VerifyCaptchaCodeAsync(user, code));
    }

    [Fact]
    public async Task EnsureCaptchaCodeAsync_AfterAFailure_DoesNotResetTheFailCount()
    {
        // The GET redisplay path must only refresh the CAPTCHA - it must not touch CountFailes/
        // TimeStamp, which are exclusively owned by the actual failure-tracking calls.
        var options = new LoginBotDetectionOptions { MaxFailCount = 2 };
        var detector = CreateDetector(options);
        const string user = "get-redisplay-after-failure-user";

        await detector.AddSuspiciousUserAsync(user);
        await detector.AddSuspiciousUserAsync(user);
        Assert.True(await detector.IsSuspiciousUserAsync(user));

        await detector.EnsureCaptchaCodeAsync(user);

        Assert.True(await detector.IsSuspiciousUserAsync(user));
    }

    [Fact]
    public async Task IsSuspiciousUserAsync_TreatsUsernameCaseInsensitively()
    {
        // Varying the casing of the submitted username must not create an independent counter -
        // otherwise an attacker trivially bypasses the fail-counter by cycling through casings.
        var options = new LoginBotDetectionOptions { MaxFailCount = 2 };
        var detector = CreateDetector(options);

        await detector.AddSuspiciousUserAsync("Attacker@Example.com");
        await detector.AddSuspiciousUserAsync("ATTACKER@EXAMPLE.COM");

        Assert.True(await detector.IsSuspiciousUserAsync("attacker@example.com"));
    }

    [Fact]
    public async Task RemoveSuspiciousUserAsync_IsCaseInsensitive()
    {
        var options = new LoginBotDetectionOptions { MaxFailCount = 1 };
        var detector = CreateDetector(options);

        await detector.AddSuspiciousUserAsync("MixedCase@Example.com");
        Assert.True(await detector.IsSuspiciousUserAsync("mixedcase@example.com"));

        await detector.RemoveSuspiciousUserAsync("MIXEDCASE@EXAMPLE.COM");

        Assert.False(await detector.IsSuspiciousUserAsync("MixedCase@Example.com"));
    }

    [Fact]
    public async Task BlockSuspicousUser_SecondConcurrentCall_ThrowsWithinBlockWindow()
    {
        var options = new LoginBotDetectionOptions { BlockSuspiciousUserSeconds = 2 };
        var detector = CreateDetector(options);
        const string user = "tarpit-user";

        // No await before the dictionary write inside BlockSuspicousUser, so by the time this call
        // returns a Task (at its first genuine await point) the block entry is already visible.
        var firstCall = detector.BlockSuspicousUser(user);

        await Assert.ThrowsAsync<StatusMessageException>(() => detector.BlockSuspicousUser(user));

        await firstCall;
    }

    [Fact]
    public async Task BlockSuspicousUser_TreatsUsernameCaseInsensitively_ForConcurrentCalls()
    {
        var options = new LoginBotDetectionOptions { BlockSuspiciousUserSeconds = 2 };
        var detector = CreateDetector(options);

        var firstCall = detector.BlockSuspicousUser("Blocked@Example.com");

        await Assert.ThrowsAsync<StatusMessageException>(() => detector.BlockSuspicousUser("BLOCKED@EXAMPLE.COM"));

        await firstCall;
    }

    [Fact]
    public async Task IsSuspiciousIpAsync_ForUnknownIp_ReturnsFalse()
    {
        var detector = CreateDetector();

        Assert.False(await detector.IsSuspiciousIpAsync("203.0.113.5"));
    }

    [Fact]
    public async Task IsSuspiciousIpAsync_BecomesSuspicious_OnlyAfterReachingMaxIpFailCount()
    {
        var options = new LoginBotDetectionOptions { MaxIpFailCount = 3 };
        var detector = CreateDetector(options);
        const string ip = "203.0.113.10";

        await detector.AddSuspiciousIpAsync(ip);
        await detector.AddSuspiciousIpAsync(ip);
        Assert.False(await detector.IsSuspiciousIpAsync(ip)); // 2 < 3

        await detector.AddSuspiciousIpAsync(ip);
        Assert.True(await detector.IsSuspiciousIpAsync(ip));  // 3 >= 3
    }

    [Fact]
    public async Task IsSuspiciousIpAsync_ForgetsStaleEntries_AfterRememberWindow()
    {
        var options = new LoginBotDetectionOptions
        {
            MaxIpFailCount = 1,
            RememberSuspiciousIpTotalMinutes = 0
        };
        var detector = CreateDetector(options);
        const string ip = "203.0.113.20";

        await detector.AddSuspiciousIpAsync(ip);

        Assert.False(await detector.IsSuspiciousIpAsync(ip));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsSuspiciousIpAsync_WithEmptyAddress_Throws(string ipAddress)
    {
        var detector = CreateDetector();

        await Assert.ThrowsAsync<ArgumentException>(() => detector.IsSuspiciousIpAsync(ipAddress));
    }

    [Fact]
    public async Task Username_And_Ip_Tracking_Use_Independent_Keyspaces()
    {
        // A cache-key collision between the username and IP namespaces would let an attacker's
        // failed-login count leak into (or be masked by) unrelated state under the same literal string.
        var options = new LoginBotDetectionOptions { MaxFailCount = 1, MaxIpFailCount = 1 };
        var detector = CreateDetector(options);
        const string value = "10.0.0.1";

        await detector.AddSuspiciousUserAsync(value);

        Assert.True(await detector.IsSuspiciousUserAsync(value));
        Assert.False(await detector.IsSuspiciousIpAsync(value));
    }

    [Fact]
    public async Task GetSuspiciousUsersAsync_ForNoActivity_ReturnsEmpty()
    {
        var detector = CreateDetector();

        Assert.Empty(await detector.GetSuspiciousUsersAsync());
    }

    [Fact]
    public async Task GetSuspiciousUsersAsync_ListsTrackedUsers_EvenBelowThreshold()
    {
        // The admin list should show users who are just getting close, not only ones already suspicious.
        var options = new LoginBotDetectionOptions { MaxFailCount = 3 };
        var detector = CreateDetector(options);

        await detector.AddSuspiciousUserAsync("below-threshold@example.com");
        await detector.AddSuspiciousUserAsync("at-threshold@example.com");
        await detector.AddSuspiciousUserAsync("at-threshold@example.com");
        await detector.AddSuspiciousUserAsync("at-threshold@example.com");

        var entries = await detector.GetSuspiciousUsersAsync();

        var below = Assert.Single(entries, e => e.Key == "BELOW-THRESHOLD@EXAMPLE.COM");
        Assert.Equal(1, below.FailCount);
        Assert.False(below.IsSuspicious);

        var at = Assert.Single(entries, e => e.Key == "AT-THRESHOLD@EXAMPLE.COM");
        Assert.Equal(3, at.FailCount);
        Assert.True(at.IsSuspicious);
    }

    [Fact]
    public async Task GetSuspiciousUsersAsync_DoesNotInclude_EnsureCaptchaCodeAsync_Entries()
    {
        // EnsureCaptchaCodeAsync (GET-redisplay path) can create a 0-failure entry purely to hold a
        // CAPTCHA code (e.g. when only the IP is suspicious) - that must not show up as a "suspicious
        // user" in the admin list, since the user themselves never actually failed anything.
        var detector = CreateDetector();

        await detector.EnsureCaptchaCodeAsync("innocent-user@example.com");

        Assert.Empty(await detector.GetSuspiciousUsersAsync());
    }

    [Fact]
    public async Task RemoveSuspiciousUserAsync_RemovesFromTheAdminList()
    {
        var detector = CreateDetector();
        const string user = "clear-me@example.com";

        await detector.AddSuspiciousUserAsync(user);
        Assert.Single(await detector.GetSuspiciousUsersAsync());

        await detector.RemoveSuspiciousUserAsync(user);

        Assert.Empty(await detector.GetSuspiciousUsersAsync());
    }

    [Fact]
    public async Task GetSuspiciousIpsAsync_ListsTrackedIps_EvenBelowThreshold()
    {
        var options = new LoginBotDetectionOptions { MaxIpFailCount = 2 };
        var detector = CreateDetector(options);

        await detector.AddSuspiciousIpAsync("203.0.113.30");
        await detector.AddSuspiciousIpAsync("203.0.113.31");
        await detector.AddSuspiciousIpAsync("203.0.113.31");

        var entries = await detector.GetSuspiciousIpsAsync();

        var below = Assert.Single(entries, e => e.Key == "203.0.113.30");
        Assert.False(below.IsSuspicious);

        var at = Assert.Single(entries, e => e.Key == "203.0.113.31");
        Assert.True(at.IsSuspicious);
    }

    [Fact]
    public async Task RemoveSuspiciousIpAsync_ClearsSuspicionAndRemovesFromTheAdminList()
    {
        // This is the admin-only escape hatch for a wrongly-flagged shared IP (e.g. a whole company
        // behind one NAT address) - unlike the login flow, an explicit admin action may clear it.
        var options = new LoginBotDetectionOptions { MaxIpFailCount = 1 };
        var detector = CreateDetector(options);
        const string ip = "203.0.113.40";

        await detector.AddSuspiciousIpAsync(ip);
        Assert.True(await detector.IsSuspiciousIpAsync(ip));
        Assert.Single(await detector.GetSuspiciousIpsAsync());

        await detector.RemoveSuspiciousIpAsync(ip);

        Assert.False(await detector.IsSuspiciousIpAsync(ip));
        Assert.Empty(await detector.GetSuspiciousIpsAsync());
    }

    [Fact]
    public async Task GetSuspiciousUsersAsync_PrunesStaleEntries_FromTheIndex()
    {
        var options = new LoginBotDetectionOptions
        {
            MaxFailCount = 1,
            RembemberSuspiciousUserTotalMinutes = 0
        };
        var detector = CreateDetector(options);

        await detector.AddSuspiciousUserAsync("stale-listed-user@example.com");

        // Immediately stale under a 0-minute remember window - listing must prune it from the index.
        Assert.Empty(await detector.GetSuspiciousUsersAsync());

        // Listing again must not error or resurrect the already-pruned entry.
        Assert.Empty(await detector.GetSuspiciousUsersAsync());
    }
}
