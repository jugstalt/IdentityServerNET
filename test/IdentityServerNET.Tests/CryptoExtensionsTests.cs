using System.Text.RegularExpressions;
using IdentityServerNET;
using IdentityServerNET.Services.Cryptography;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for <see cref="CryptoExtensions.NameToHexId"/>, the deterministic
/// pseudonymisation used to derive a stable, non-reversible-looking id from a user name.
/// The id must be stable, normalised (trim / case-insensitive) and collision-free for
/// distinct inputs, while never echoing the original name.
/// </summary>
public class CryptoExtensionsTests
{
    [Fact]
    public void NameToHexId_IsDeterministic()
    {
        var id1 = "alice.smith".NameToHexId();
        var id2 = "alice.smith".NameToHexId();

        Assert.Equal(id1, id2);
    }

    [Theory]
    [InlineData("alice")]
    [InlineData("ALICE")]
    [InlineData("  Alice  ")]
    [InlineData("aLiCe")]
    public void NameToHexId_NormalisesCaseAndWhitespace(string name)
    {
        // All variants must map to the same id as the canonical lower-case form.
        Assert.Equal("alice".NameToHexId(), name.NameToHexId());
    }

    [Fact]
    public void NameToHexId_ProducesDifferentIdsForDifferentNames()
    {
        Assert.NotEqual("alice".NameToHexId(), "bob".NameToHexId());
    }

    [Fact]
    public void NameToHexId_DoesNotLeakOriginalName()
    {
        const string name = "secret-user-name";

        var id = name.NameToHexId();

        Assert.DoesNotContain("secret-user-name", id);
        Assert.NotEqual(name, id);
    }

    [Fact]
    public void NameToHexId_ReturnsLowercaseHexString()
    {
        var id = "alice".NameToHexId();

        Assert.Matches(new Regex("^[0-9a-f]+$"), id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void NameToHexId_ForBlankInput_ReturnsEmpty(string? name)
    {
        Assert.Equal(string.Empty, name!.NameToHexId());
    }

    [Fact]
    public void NameToHexId_WithExplicitCryptoService_MatchesDefault()
    {
        // The default implementation uses Base64CryptoService; passing it explicitly
        // must yield the identical id.
        var withDefault = "alice".NameToHexId();
        var withExplicit = "alice".NameToHexId(new Base64CryptoService());

        Assert.Equal(withDefault, withExplicit);
    }
}
