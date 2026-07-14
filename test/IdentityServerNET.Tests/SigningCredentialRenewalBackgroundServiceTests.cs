using System;
using System.Threading;
using System.Threading.Tasks;
using IdentityServerNET.Services.SigningCredential;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Tests for <see cref="SigningCredentialRenewalBackgroundService"/>: this is what makes certificate
/// renewal happen periodically at runtime instead of only once at application startup, so a
/// long-running instance rotates signing keys without needing a restart.
/// </summary>
public class SigningCredentialRenewalBackgroundServiceTests
{
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(10);

    private static SigningCredentialRenewalBackgroundService CreateService(
            FakeSigningCredentialCertificateStorage storage,
            TimeSpan checkInterval)
        => new SigningCredentialRenewalBackgroundService(
            storage,
            Options.Create(new SigningCredentialRenewalOptions { CheckInterval = checkInterval }),
            NullLogger<SigningCredentialRenewalBackgroundService>.Instance);

    [Fact]
    public async Task ExecuteAsync_CallsRenewCertificatesAsync_Periodically()
    {
        var storage = new FakeSigningCredentialCertificateStorage();
        var service = CreateService(storage, TimeSpan.FromMilliseconds(10));

        await service.StartAsync(CancellationToken.None);
        try
        {
            // Actively wait for two ticks instead of sleeping a fixed duration - robust regardless
            // of how much the timer gets delayed by system load during a parallel test run.
            await storage.WaitForRenewCallsAsync(count: 2, WaitTimeout);
        }
        finally
        {
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(storage.RenewCallCount >= 2);
    }

    [Fact]
    public async Task ExecuteAsync_SwallowsExceptions_AndKeepsTicking()
    {
        var storage = new FakeSigningCredentialCertificateStorage
        {
            ThrowOnRenew = new InvalidOperationException("simulated storage failure")
        };
        var service = CreateService(storage, TimeSpan.FromMilliseconds(10));

        await service.StartAsync(CancellationToken.None);
        try
        {
            // If a failing tick stopped the loop, this would time out instead of completing.
            await storage.WaitForRenewCallsAsync(count: 2, WaitTimeout);
        }
        finally
        {
            // Must not throw: a failing renewal tick must not crash the host.
            await service.StopAsync(CancellationToken.None);
        }

        Assert.True(
            storage.RenewCallCount >= 2,
            "A failing tick must be logged and swallowed, not stop subsequent ticks.");
    }
}
