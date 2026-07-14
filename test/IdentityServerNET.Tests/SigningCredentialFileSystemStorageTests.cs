using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using IdentityServerNET.Services.SigningCredential;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Tests for <see cref="SigningCredentialCertificateFileSystemStorage"/>: certificate creation/rotation,
/// the 60-day active window used by <c>GetCertificatesAsync</c>, and the retention-based cleanup of
/// certificate files that are no longer active (introduced to stop old .pfx files from accumulating
/// on disk forever).
/// </summary>
public class SigningCredentialFileSystemStorageTests
{
    private const string Password = "unit-test-cert-password";

    private static SigningCredentialCertificateFileSystemStorage CreateStorage(string storagePath) =>
        new SigningCredentialCertificateFileSystemStorage(
            Options.Create(new SigningCredentialCertificateStorageOptions
            {
                Storage = storagePath,
                CertPassword = Password
            }),
            new TestCertificateFactory());

    // Writes a .pfx file directly (bypassing RenewCertificatesAsync's own "do I need to rotate?"
    // check) so tests can precisely control the file's on-disk age via File.SetCreationTime.
    private static string WriteCertificateFile(string directory, DateTime creationTime)
    {
        var serializer = new SimpleCertificateSerializer(new SigningCredentialCertificateStorageOptions
        {
            Storage = directory,
            CertPassword = Password
        });

        using var cert = TestCertificateFactory.Create(Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, $"{Guid.NewGuid():N}.pfx");
        var bytes = serializer.WriteToBytes(cert, X509ContentType.Pfx, "name");
        File.WriteAllBytes(path, bytes);
        File.SetCreationTime(path, creationTime);
        File.SetLastWriteTime(path, creationTime);

        return path;
    }

    [Fact]
    public void Constructor_CreatesStorageDirectory_WhenMissing()
    {
        using var temp = new TempDirectory();
        var missingSubdir = Path.Combine(temp.Path, "validation");

        Assert.False(Directory.Exists(missingSubdir));

        CreateStorage(missingSubdir);

        Assert.True(Directory.Exists(missingSubdir));
    }

    [Fact]
    public async Task RenewCertificatesAsync_CreatesUsableCertificate_WhenStorageIsEmpty()
    {
        using var temp = new TempDirectory();
        var storage = CreateStorage(temp.Path);

        await storage.RenewCertificatesAsync();
        var certificates = (await storage.GetCertificatesAsync()).ToList();

        Assert.Single(certificates);
        Assert.True(certificates[0].HasPrivateKey, "A signing certificate must have a private key.");
        Assert.Single(Directory.GetFiles(temp.Path, "*.pfx"));
    }

    [Fact]
    public async Task RenewCertificatesAsync_DoesNotCreateNewCertificate_WhileRecentCertificateExists()
    {
        using var temp = new TempDirectory();
        var storage = CreateStorage(temp.Path);

        await storage.RenewCertificatesAsync();
        await storage.RenewCertificatesAsync();

        Assert.Single(Directory.GetFiles(temp.Path, "*.pfx"));
    }

    [Fact]
    public async Task RenewCertificatesAsync_CreatesNewCertificate_WhenExistingCertificateIsOlderThanThreshold()
    {
        using var temp = new TempDirectory();
        WriteCertificateFile(temp.Path, DateTime.Now.AddDays(-61));
        var storage = CreateStorage(temp.Path);

        await storage.RenewCertificatesAsync(ifOlderThanDays: 60);

        Assert.Equal(2, Directory.GetFiles(temp.Path, "*.pfx").Length);
    }

    [Fact]
    public async Task GetCertificatesAsync_ExcludesFiles_OlderThanActiveWindow()
    {
        using var temp = new TempDirectory();
        // Older than the 60-day active window, but younger than the 180-day retention window -
        // must disappear from the active set while the file itself stays on disk.
        WriteCertificateFile(temp.Path, DateTime.Now.AddDays(-90));
        var storage = CreateStorage(temp.Path);

        var certificates = await storage.GetCertificatesAsync();

        Assert.Empty(certificates);
        Assert.Single(Directory.GetFiles(temp.Path, "*.pfx"));
    }

    [Fact]
    public async Task RenewCertificatesAsync_KeepsCertificateFile_WithinRetentionWindow()
    {
        using var temp = new TempDirectory();
        // Outside the 60-day active window but inside the (60 * 3 = 180-day) retention window.
        var path = WriteCertificateFile(temp.Path, DateTime.Now.AddDays(-100));
        var storage = CreateStorage(temp.Path);

        await storage.RenewCertificatesAsync(ifOlderThanDays: 60);

        Assert.True(File.Exists(path), "A file inside the retention window must not be deleted.");
    }

    [Fact]
    public async Task RenewCertificatesAsync_DeletesCertificateFile_OlderThanRetentionWindow()
    {
        using var temp = new TempDirectory();
        // Older than the (60 * 3 = 180-day) retention window.
        var path = WriteCertificateFile(temp.Path, DateTime.Now.AddDays(-181));
        var storage = CreateStorage(temp.Path);

        await storage.RenewCertificatesAsync(ifOlderThanDays: 60);

        Assert.False(File.Exists(path), "A file past the retention window must be deleted.");
    }

    [Fact]
    public async Task GetCertificateAsync_ResolvesCertificateBySubject()
    {
        using var temp = new TempDirectory();
        var storage = CreateStorage(temp.Path);
        await storage.RenewCertificatesAsync();
        var cert = (await storage.GetCertificatesAsync()).First();

        var resolved = await storage.GetCertificateAsync(cert.Subject);

        Assert.NotNull(resolved);
        Assert.Equal(cert.Thumbprint, resolved!.Thumbprint);
    }

    [Fact]
    public async Task GetCertificateAsync_ForUnknownSubject_ReturnsNull()
    {
        using var temp = new TempDirectory();
        var storage = CreateStorage(temp.Path);

        var resolved = await storage.GetCertificateAsync("cn=does-not-exist");

        Assert.Null(resolved);
    }
}
