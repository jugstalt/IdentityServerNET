using Xunit;

namespace IdentityServerNET.Host.Tests;

/// <summary>
/// Groups all tests that boot the real host into a single xUnit collection so they share one
/// <see cref="HostApplicationFactory"/> instance (hence one booted host and one storage
/// directory) and never run in parallel against each other.
///
/// This is required because the host persists its signing credential to the file system and the
/// serializer opens those files with <c>FileShare.None</c>; two hosts booting concurrently against
/// the process-global storage path would otherwise race on those files. Sharing a single host also
/// makes the suite faster, since the (relatively heavy) production startup runs only once.
/// </summary>
[CollectionDefinition(Name)]
public sealed class HostTestCollection : ICollectionFixture<HostApplicationFactory>
{
    public const string Name = "Host collection";
}
