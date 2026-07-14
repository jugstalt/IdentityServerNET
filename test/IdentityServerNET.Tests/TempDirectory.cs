using System;
using System.IO;

namespace IdentityServerNET.Tests;

/// <summary>
/// A uniquely-named temp directory created on construction and recursively removed on
/// <see cref="Dispose"/>. Used by tests that exercise file-system-backed storage.
/// </summary>
internal sealed class TempDirectory : IDisposable
{
    public string Path { get; }

    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"isnet-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path))
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
