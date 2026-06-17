using Microsoft.AspNetCore.Mvc.Testing;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Boots the real IdentityServer host (<see cref="Program"/>) in-memory for smoke testing.
///
/// The host reads several settings (storage root, HTTPS redirection, hosting environment)
/// inside its top-level startup code – i.e. BEFORE <c>builder.Build()</c> runs – so the usual
/// <see cref="WebApplicationFactory{TEntryPoint}.ConfigureWebHost"/> hooks are applied too late
/// to influence them. The only reliable way to steer that early code is via environment
/// variables, which <c>WebApplication.CreateBuilder</c> picks up immediately. We therefore:
/// <list type="bullet">
///   <item>redirect <c>IdentityServer:StorageRootPath</c> to an isolated temp directory so the
///   signing certificates, data-protection keys and in-memory stores never touch the machine's
///   default location (e.g. <c>C:\apps\identityserver-net</c>);</item>
///   <item>disable HTTPS redirection so the in-memory test transport (plain HTTP) is not 307'd;</item>
///   <item>pin the hosting environment to Development.</item>
/// </list>
/// The temp directory is removed on dispose to keep test runs self-contained.
/// </summary>
public sealed class HostApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _storageRoot;

    public HostApplicationFactory()
    {
        _storageRoot = Path.Combine(Path.GetTempPath(), $"isnet-host-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_storageRoot);

        // Must be set before the host's top-level startup code reads them.
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("IdentityServer__StorageRootPath", _storageRoot);
        Environment.SetEnvironmentVariable("IdentityServer__UseHttpsRedirection", "false");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            try
            {
                if (Directory.Exists(_storageRoot))
                {
                    Directory.Delete(_storageRoot, recursive: true);
                }
            }
            catch
            {
                // Best-effort cleanup; a leftover temp directory must never fail a test run.
            }
        }
    }
}
