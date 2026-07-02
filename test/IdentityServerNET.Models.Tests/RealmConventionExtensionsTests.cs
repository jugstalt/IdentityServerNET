using IdentityServerNET.Models.Extensions;
using Xunit;

namespace IdentityServerNET.Models.Tests;

/// <summary>
/// Pins the realm naming convention (<c>{name}@{realm}</c>). Because the whole multi-tenancy
/// isolation model relies on these parse/compose helpers being unambiguous, the allow/deny rules
/// are locked down here — especially the guarantee that an e-mail-shaped value is NEVER mistaken
/// for a realm-scoped identifier.
/// </summary>
public class RealmConventionExtensionsTests
{
    #region HasRealm / GetRealm

    [Theory]
    [InlineData("my-client@xyz", "xyz")]
    [InlineData("my-role@acme", "acme")]
    [InlineData("res@a-b-c", "a-b-c")]
    public void GetRealm_ExtractsSlug_FromRealmScopedId(string id, string expectedRealm)
    {
        Assert.True(id.HasRealm());
        Assert.Equal(expectedRealm, id.GetRealm());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my-client")]        // global, no separator
    [InlineData("@xyz")]             // nothing before the separator
    [InlineData("my-client@")]       // nothing after the separator
    [InlineData("user@foo.com")]     // e-mail shaped: dotted domain is not a realm slug
    [InlineData("a@b.c.d")]          // multiple dots -> not a realm slug
    [InlineData("name@UPPER")]       // uppercase suffix is not a valid slug shape
    public void GetRealm_ReturnsNull_ForNonRealmScopedValues(string? id)
    {
        Assert.False(id.HasRealm());
        Assert.Null(id.GetRealm());
    }

    #endregion

    #region GetRealmScopedName / RemoveRealmNamespace

    [Theory]
    [InlineData("my-client@xyz", "my-client")]
    [InlineData("my-client", "my-client")]     // unchanged when global
    [InlineData("user@foo.com", "user@foo.com")] // unchanged: e-mail is not realm-scoped
    [InlineData("", "")]
    public void GetRealmScopedName_ReturnsLocalPart(string? id, string expected)
    {
        Assert.Equal(expected, id.GetRealmScopedName());
        Assert.Equal(expected, id.RemoveRealmNamespace());
    }

    #endregion

    #region AddRealmNamespace

    [Theory]
    [InlineData("my-client", "xyz", "my-client@xyz")]
    [InlineData("my-client", null, "my-client")]  // no realm -> global namespace
    [InlineData("my-client", "", "my-client")]
    public void AddRealmNamespace_Composes(string name, string? realm, string expected)
    {
        Assert.Equal(expected, name.AddRealmNamespace(realm));
    }

    [Fact]
    public void AddRealmNamespace_IsIdempotent_NeverDoublesTheSuffix()
    {
        var once = "my-client".AddRealmNamespace("xyz");
        var twice = once.AddRealmNamespace("xyz");

        Assert.Equal("my-client@xyz", twice);
    }

    [Fact]
    public void AddRealmNamespace_ReplacesExistingRealm()
    {
        Assert.Equal("my-client@acme", "my-client@xyz".AddRealmNamespace("acme"));
    }

    #endregion

    #region BelongsToRealm

    [Theory]
    [InlineData("my-client@xyz", "xyz", true)]
    [InlineData("my-client@xyz", "acme", false)]
    [InlineData("my-client@xyz", null, false)]  // realm-scoped id does not belong to global
    [InlineData("my-client", null, true)]       // global id belongs to the null realm
    [InlineData("my-client", "xyz", false)]
    public void BelongsToRealm_MatchesRealm(string id, string? realm, bool expected)
    {
        Assert.Equal(expected, id.BelongsToRealm(realm));
    }

    #endregion

    #region Reserved names

    [Theory]
    [InlineData("openid")]
    [InlineData("profile")]
    [InlineData("email")]
    [InlineData("offline_access")]
    [InlineData("OpenId")]        // case-insensitive
    public void IsGlobalReservedName_AcceptsStandardScopes(string name)
    {
        Assert.True(name.IsGlobalReservedName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("my-api")]
    [InlineData("openidish")]
    public void IsGlobalReservedName_RejectsCustomNames(string? name)
    {
        Assert.False(name.IsGlobalReservedName());
    }

    #endregion

    #region ClientAllowsUserRealm (runtime cross-realm guard)

    [Theory]
    [InlineData("my-client@xyz", "xyz", true)]   // realm client, matching user realm
    [InlineData("my-client@xyz", "acme", false)] // realm client, foreign user realm
    [InlineData("my-client@xyz", null, false)]   // realm client, global user
    [InlineData("my-client", "xyz", true)]       // global client, realm user -> allowed
    [InlineData("my-client", null, true)]        // global client, global user
    public void ClientAllowsUserRealm_EnforcesTheRule(string clientId, string? userRealm, bool expected)
    {
        Assert.Equal(expected, clientId.ClientAllowsUserRealm(userRealm));
    }

    #endregion

    #region Validation

    [Theory]
    [InlineData("xyz")]
    [InlineData("acme-corp")]
    public void IsValidRealmName_AcceptsSlugs(string realm)
    {
        Assert.True(realm.IsValidRealmName());
    }

    [Theory]
    [InlineData("ab")]           // too short
    [InlineData("Foo")]          // uppercase
    [InlineData("foo.com")]      // dot not allowed
    [InlineData("-foo")]
    public void IsValidRealmName_RejectsInvalidSlugs(string realm)
    {
        Assert.False(realm.IsValidRealmName());
    }

    [Theory]
    [InlineData("my-client")]
    [InlineData("some.resource")] // dots are fine in a local name (only '@' is forbidden)
    public void IsValidRealmScopedName_AcceptsNamesWithoutSeparator(string localName)
    {
        Assert.True(localName.IsValidRealmScopedName());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("my-client@xyz")] // already contains the separator
    public void IsValidRealmScopedName_RejectsEmptyOrSeparatorContaining(string? localName)
    {
        Assert.False(localName!.IsValidRealmScopedName());
    }

    #endregion
}
