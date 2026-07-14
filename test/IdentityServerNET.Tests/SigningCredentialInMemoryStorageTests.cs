using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IdentityServerNET.Services.SigningCredential;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for <see cref="SigningCredentialCertificateInMemoryStorage"/>, the rotation
/// store for token-signing certificates. They verify that a usable signing certificate (with a
/// private key and a sufficiently strong RSA key) is created, can be resolved by its subject and
/// is not needlessly rotated while a recent certificate already exists.
/// </summary>
public class SigningCredentialInMemoryStorageTests
{
    private static SigningCredentialCertificateInMemoryStorage CreateStorage() =>
        new SigningCredentialCertificateInMemoryStorage(new TestCertificateFactory());

    [Fact]
    public async Task RenewCertificatesAsync_CreatesAtLeastOneUsableSigningCertificate()
    {
        var storage = CreateStorage();

        await storage.RenewCertificatesAsync();
        var certificates = (await storage.GetCertificatesAsync()).ToList();

        Assert.NotEmpty(certificates);

        var cert = certificates.First();
        Assert.True(cert.HasPrivateKey, "A signing certificate must have a private key.");

        using var rsa = cert.GetRSAPublicKey();
        Assert.NotNull(rsa);
        Assert.True(rsa!.KeySize >= 2048, "Signing key must be at least 2048 bit.");
    }

    [Fact]
    public async Task GetCertificateAsync_ResolvesCertificateBySubject()
    {
        var storage = CreateStorage();
        await storage.RenewCertificatesAsync();
        var cert = (await storage.GetCertificatesAsync()).First();

        var resolved = await storage.GetCertificateAsync(cert.Subject);

        Assert.NotNull(resolved);
        Assert.Equal(cert.Thumbprint, resolved!.Thumbprint);
    }

    [Fact]
    public async Task RenewCertificatesAsync_DoesNotRotate_WhileRecentCertificateExists()
    {
        var storage = CreateStorage();

        await storage.RenewCertificatesAsync();
        var countAfterFirst = (await storage.GetCertificatesAsync()).Count();

        await storage.RenewCertificatesAsync();
        var countAfterSecond = (await storage.GetCertificatesAsync()).Count();

        Assert.Equal(countAfterFirst, countAfterSecond);
    }

    [Fact]
    public async Task GetCertificateAsync_ForUnknownSubject_ReturnsNullCertificate()
    {
        // For an unknown subject the implementation returns a completed Task whose result is
        // null (Task.FromResult&lt;X509Certificate2&gt;(null)). The CN value "1" is parsed to the
        // numeric key 1, which can never collide with the DateTime.Now.Ticks keys used for real
        // certificates, so this characterization is deterministic regardless of test order.
        var storage = CreateStorage();

        var resolved = await storage.GetCertificateAsync("CN=1");

        Assert.Null(resolved);
    }

    [Fact]
    public async Task RenewCertificatesAsync_PrunesEntries_OlderThanRetentionWindow()
    {
        // The backing dictionary is a private static field with no public way to inject an
        // artificially old entry, so reflection is used to set up this one scenario.
        var storage = CreateStorage();
        await storage.RenewCertificatesAsync(); // ensures the static dictionary is initialized

        var certificates = GetCertificatesField();
        var staleKey = DateTime.Now.AddDays(-200).Ticks;
        using var staleCert = TestCertificateFactory.Create("stale-entry");
        certificates[staleKey] = staleCert;

        await storage.RenewCertificatesAsync(ifOlderThanDays: 60);

        Assert.False(
            certificates.ContainsKey(staleKey),
            "Entries past the retention window (ifOlderThanDays * 3 = 180 days) must be pruned.");
    }

    private static ConcurrentDictionary<long, X509Certificate2> GetCertificatesField()
    {
        var field = typeof(SigningCredentialCertificateInMemoryStorage)
            .GetField("_certificates", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(field);

        return (ConcurrentDictionary<long, X509Certificate2>)field!.GetValue(null)!;
    }
}
