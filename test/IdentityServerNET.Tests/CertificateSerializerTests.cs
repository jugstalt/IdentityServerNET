using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using IdentityServerNET.Services.SigningCredential;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for <see cref="SimpleCertificateSerializer"/>, which persists the signing
/// credential (including its private key) as a password-protected PKCS#12/PFX blob. The tests
/// verify lossless round-tripping of the private key and that the exported material is actually
/// encrypted with the configured password.
/// </summary>
public class CertificateSerializerTests : IDisposable
{
    private const string Password = "unit-test-cert-password";
    private readonly X509Certificate2 _cert;

    public CertificateSerializerTests()
    {
        _cert = TestCertificateFactory.Create("isnet-signing-test");
    }

    public void Dispose() => _cert.Dispose();

    private static SimpleCertificateSerializer CreateSerializer(string password) =>
        new SimpleCertificateSerializer(new SigningCredentialCertificateStorageOptions
        {
            CertPassword = password,
            Storage = Path.GetTempPath()
        });

    // Importing a PKCS#12 into the machine key store (MachineKeySet|PersistKeySet) requires
    // privileges that are not available in every environment (e.g. locked-down CI agents) and
    // is not supported on every platform. The serializer uses those flags internally, so we
    // probe support once and only exercise the serializer's own loaders where it is available.
    // The core round-trip is always verified with EphemeralKeySet, which works everywhere.
    private static readonly bool MachineKeySetSupported = ProbeMachineKeySetSupport();

    private static bool ProbeMachineKeySetSupport()
    {
        try
        {
            using var probeCert = TestCertificateFactory.Create("machinekeyset-probe", 1);
            var pfx = probeCert.Export(X509ContentType.Pfx, "probe");
            using var loaded = X509CertificateLoader.LoadPkcs12(
                pfx,
                "probe",
                X509KeyStorageFlags.MachineKeySet
                | X509KeyStorageFlags.PersistKeySet
                | X509KeyStorageFlags.Exportable);
            return true;
        }
        catch
        {
            return false;
        }
    }

    [Fact]
    public void WriteToBytes_ProducesNonEmptyPkcs12()
    {
        var serializer = CreateSerializer(Password);

        var bytes = serializer.WriteToBytes(_cert, X509ContentType.Pfx, "name");

        Assert.NotNull(bytes);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void WriteToBytes_ExportIsProtectedByConfiguredPassword()
    {
        var serializer = CreateSerializer(Password);
        var bytes = serializer.WriteToBytes(_cert, X509ContentType.Pfx, "name");

        // Loading with the correct password succeeds and yields the same certificate.
        using var reloaded = X509CertificateLoader.LoadPkcs12(
            bytes, Password, X509KeyStorageFlags.EphemeralKeySet);
        Assert.Equal(_cert.Thumbprint, reloaded.Thumbprint);

        // Loading with a wrong password must fail – proving the key material is encrypted.
        Assert.ThrowsAny<CryptographicException>(() =>
            X509CertificateLoader.LoadPkcs12(bytes, "wrong-password", X509KeyStorageFlags.EphemeralKeySet));
    }

    [Fact]
    public void WriteToBytes_LoadFromBytes_RoundTripPreservesIdentityAndPrivateKey()
    {
        var serializer = CreateSerializer(Password);

        var bytes = serializer.WriteToBytes(_cert, X509ContentType.Pfx, "name");

        // Environment-independent check: the exported PFX is a valid, password-protected PKCS#12
        // whose private key survives the round-trip. EphemeralKeySet works in every environment.
        using (var reloaded = X509CertificateLoader.LoadPkcs12(
            bytes, Password, X509KeyStorageFlags.EphemeralKeySet))
        {
            Assert.Equal(_cert.Thumbprint, reloaded.Thumbprint);
            Assert.Equal(_cert.Subject, reloaded.Subject);
            Assert.True(reloaded.HasPrivateKey, "The private key must survive serialization.");
        }

        // Additionally exercise the serializer's own loader where the platform permits it.
        if (MachineKeySetSupported)
        {
            using var loaded = serializer.LoadFromBytes(bytes, "name");
            Assert.Equal(_cert.Thumbprint, loaded.Thumbprint);
            Assert.True(loaded.HasPrivateKey);
        }
    }

    [Fact]
    public async Task WriteToFileAsync_RoundTrips()
    {
        var serializer = CreateSerializer(Password);
        var path = Path.Combine(Path.GetTempPath(), $"isnet-cert-test-{Guid.NewGuid():N}.pfx");

        try
        {
            await serializer.WriteToFileAsync(path, _cert, X509ContentType.Pfx);

            Assert.True(File.Exists(path));

            // Environment-independent verification of the written file.
            var fileBytes = await File.ReadAllBytesAsync(path);
            using (var reloaded = X509CertificateLoader.LoadPkcs12(
                fileBytes, Password, X509KeyStorageFlags.EphemeralKeySet))
            {
                Assert.Equal(_cert.Thumbprint, reloaded.Thumbprint);
                Assert.True(reloaded.HasPrivateKey);
            }

            // Additionally exercise the serializer's own file loader where the platform permits it.
            if (MachineKeySetSupported)
            {
                using var loaded = await serializer.LoadFromFileAsync(path);
                Assert.Equal(_cert.Thumbprint, loaded.Thumbprint);
                Assert.True(loaded.HasPrivateKey);
            }
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
