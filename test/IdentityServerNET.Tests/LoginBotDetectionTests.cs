using System;
using System.Threading.Tasks;
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
}
