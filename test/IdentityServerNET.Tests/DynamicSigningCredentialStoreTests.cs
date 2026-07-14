using System;
using System.Linq;
using System.Threading.Tasks;
using IdentityServerNET.Services.SigningCredential;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Tests for <see cref="DynamicSigningCredentialStore"/>, which replaced IdentityServer4's static
/// <c>InMemorySigningCredentialsStore</c>/<c>InMemoryValidationKeysStore</c> snapshot: it re-reads
/// certificates from <see cref="Abstractions.SigningCredential.ISigningCredentialCertificateStorage"/>
/// on a short cache instead of a frozen startup snapshot, so a certificate created later by
/// <see cref="SigningCredentialRenewalBackgroundService"/> is picked up without an app restart.
/// </summary>
public class DynamicSigningCredentialStoreTests
{
    private static DynamicSigningCredentialStore CreateStore(
            FakeSigningCredentialCertificateStorage storage,
            TimeSpan? cacheDuration = null)
        => new DynamicSigningCredentialStore(
            storage,
            Options.Create(new SigningCredentialRenewalOptions
            {
                CacheDuration = cacheDuration ?? TimeSpan.FromMinutes(15)
            }));

    [Fact]
    public async Task GetSigningCredentialsAsync_ReturnsNull_WhenStorageHasNoCertificates()
    {
        var storage = new FakeSigningCredentialCertificateStorage();
        var store = CreateStore(storage);

        var credentials = await store.GetSigningCredentialsAsync();

        Assert.Null(credentials);
    }

    [Fact]
    public async Task GetSigningCredentialsAsync_UsesNewestCertificateAsSigningKey()
    {
        using var older = TestCertificateFactory.Create("older", notBefore: DateTimeOffset.Now.AddDays(-10));
        using var newer = TestCertificateFactory.Create("newer", notBefore: DateTimeOffset.Now.AddDays(-1));

        var storage = new FakeSigningCredentialCertificateStorage();
        storage.SetCertificates(older, newer);
        var store = CreateStore(storage);

        var credentials = await store.GetSigningCredentialsAsync();

        var key = Assert.IsType<X509SecurityKey>(credentials!.Key);
        Assert.Equal(newer.Thumbprint, key.Certificate.Thumbprint);
    }

    [Fact]
    public async Task GetValidationKeysAsync_ReturnsAKeyForEveryCertificate()
    {
        using var first = TestCertificateFactory.Create("first", notBefore: DateTimeOffset.Now.AddDays(-10));
        using var second = TestCertificateFactory.Create("second", notBefore: DateTimeOffset.Now.AddDays(-1));

        var storage = new FakeSigningCredentialCertificateStorage();
        storage.SetCertificates(first, second);
        var store = CreateStore(storage);

        var keys = (await store.GetValidationKeysAsync()).ToList();

        Assert.Equal(2, keys.Count);
        var thumbprints = keys.Select(k => ((X509SecurityKey)k.Key).Certificate.Thumbprint).ToHashSet();
        Assert.Contains(first.Thumbprint, thumbprints);
        Assert.Contains(second.Thumbprint, thumbprints);
    }

    [Fact]
    public async Task GetSigningCredentialsAsync_DoesNotReReadStorage_WithinCacheDuration()
    {
        using var first = TestCertificateFactory.Create("first");
        using var second = TestCertificateFactory.Create("second");

        var storage = new FakeSigningCredentialCertificateStorage();
        storage.SetCertificates(first);
        var store = CreateStore(storage, cacheDuration: TimeSpan.FromMinutes(15));

        var initial = await store.GetSigningCredentialsAsync();

        // Swap the certificate the storage would serve - the store should not notice yet.
        storage.SetCertificates(second);
        var stillCached = await store.GetSigningCredentialsAsync();

        var initialKey = (X509SecurityKey)initial!.Key;
        var stillCachedKey = (X509SecurityKey)stillCached!.Key;
        Assert.Equal(initialKey.Certificate.Thumbprint, stillCachedKey.Certificate.Thumbprint);
        Assert.Equal(first.Thumbprint, stillCachedKey.Certificate.Thumbprint);
    }

    [Fact]
    public async Task GetSigningCredentialsAsync_RefreshesFromStorage_AfterCacheExpires()
    {
        using var first = TestCertificateFactory.Create("first");
        using var second = TestCertificateFactory.Create("second");

        var storage = new FakeSigningCredentialCertificateStorage();
        storage.SetCertificates(first);
        var store = CreateStore(storage, cacheDuration: TimeSpan.FromMilliseconds(20));

        await store.GetSigningCredentialsAsync();

        storage.SetCertificates(second);
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        var refreshed = await store.GetSigningCredentialsAsync();
        var refreshedKey = (X509SecurityKey)refreshed!.Key;

        Assert.Equal(second.Thumbprint, refreshedKey.Certificate.Thumbprint);
    }

    [Fact]
    public async Task GetSigningCredentialsAsync_KeepsPreviousSnapshot_WhenStorageBecomesEmpty()
    {
        using var cert = TestCertificateFactory.Create("only-cert");

        var storage = new FakeSigningCredentialCertificateStorage();
        storage.SetCertificates(cert);
        var store = CreateStore(storage, cacheDuration: TimeSpan.FromMilliseconds(20));

        var initial = await store.GetSigningCredentialsAsync();
        Assert.NotNull(initial);

        storage.SetCertificates(); // storage now returns no certificates at all
        await Task.Delay(TimeSpan.FromMilliseconds(100));

        var afterEmpty = await store.GetSigningCredentialsAsync();

        Assert.NotNull(afterEmpty);
        var key = (X509SecurityKey)afterEmpty!.Key;
        Assert.Equal(cert.Thumbprint, key.Certificate.Thumbprint);
    }
}
