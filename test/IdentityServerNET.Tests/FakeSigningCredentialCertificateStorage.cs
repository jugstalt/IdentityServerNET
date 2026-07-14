using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using IdentityServerNET.Abstractions.SigningCredential;

namespace IdentityServerNET.Tests;

/// <summary>
/// Test double for <see cref="ISigningCredentialCertificateStorage"/> that serves a fixed,
/// swappable list of certificates and counts/optionally fails <see cref="RenewCertificatesAsync"/>
/// calls, so callers (<c>DynamicSigningCredentialStore</c>, <c>SigningCredentialRenewalBackgroundService</c>)
/// can be tested without touching the file system.
/// </summary>
internal sealed class FakeSigningCredentialCertificateStorage : ISigningCredentialCertificateStorage
{
    private int _renewCallCount;
    private List<X509Certificate2> _certificates = new();
    private readonly SemaphoreSlim _renewCallSignal = new(0);

    public int RenewCallCount => Volatile.Read(ref _renewCallCount);

    public Exception? ThrowOnRenew { get; set; }

    public void SetCertificates(params X509Certificate2[] certificates) => _certificates = certificates.ToList();

    public Task RenewCertificatesAsync(int ifOlderThanDays = 60)
    {
        Interlocked.Increment(ref _renewCallCount);
        _renewCallSignal.Release();

        if (ThrowOnRenew is { } exception)
        {
            throw exception;
        }

        return Task.CompletedTask;
    }

    // Waits until RenewCertificatesAsync has been called at least `count` times, or the timeout
    // elapses. Actively waiting for the real condition (rather than sleeping a fixed duration and
    // hoping enough timer ticks fired) keeps background-service tests reliable under system load.
    public async Task WaitForRenewCallsAsync(int count, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        while (RenewCallCount < count)
        {
            await _renewCallSignal.WaitAsync(cts.Token);
        }
    }

    public Task<IEnumerable<X509Certificate2>> GetCertificatesAsync()
        => Task.FromResult<IEnumerable<X509Certificate2>>(_certificates.ToArray());

    public Task<X509Certificate2> GetRandomCertificateAsync(int maxAgeInDays)
        => Task.FromResult(_certificates.FirstOrDefault())!;

    public Task<X509Certificate2> GetCertificateAsync(string subject)
        => Task.FromResult(_certificates.FirstOrDefault(c => c.Subject == subject))!;
}
