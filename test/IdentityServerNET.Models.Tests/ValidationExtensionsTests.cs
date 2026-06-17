using IdentityServerNET.Models.Extensions;
using Xunit;

namespace IdentityServerNET.Models.Tests;

/// <summary>
/// Security tests for the input-validation helpers. Strict validation of user-supplied input
/// (usernames, e-mail addresses, namespaces, promotion codes) is a first line of defence
/// against injection and abuse, so the allow/deny rules are pinned by these tests.
/// </summary>
public class ValidationExtensionsTests
{
    #region Username

    [Theory]
    [InlineData("alice123")]    // exactly 8 chars
    [InlineData("john-doe-99")]
    [InlineData("user_name")]
    [InlineData("AbCdEfGhIj")]
    public void IsValidGeneralUsername_AcceptsWellFormedUsernames(string username)
    {
        Assert.True(username.IsValidGeneralUsername());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("short12")]                 // 7 chars -> too short
    [InlineData("this_username_is_far_too_long")] // > 20 chars
    [InlineData("-leadinghyphen")]           // leading separator
    [InlineData("trailinghyphen-")]          // trailing separator
    [InlineData("double--hyphen")]           // consecutive separators
    [InlineData("with space")]               // whitespace not allowed
    [InlineData("with.dot.name")]            // dot not allowed
    public void IsValidGeneralUsername_RejectsInvalidUsernames(string? username)
    {
        Assert.False(username!.IsValidGeneralUsername());
    }

    #endregion

    #region E-mail

    [Theory]
    [InlineData("alice@example.com")]
    [InlineData("a.b@test.co")]
    [InlineData("bob_smith@mail.example.com")]
    public void IsValidEmailAddress_AcceptsWellFormedAddresses(string email)
    {
        Assert.True(email.IsValidEmailAddress());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("plainaddress")]
    [InlineData("@example.com")]
    [InlineData("alice@")]
    [InlineData("alice@@example.com")]
    [InlineData("alice@nodot")]
    public void IsValidEmailAddress_RejectsMalformedAddresses(string? email)
    {
        Assert.False(email!.IsValidEmailAddress());
    }

    #endregion

    #region Namespace

    [Theory]
    [InlineData("abc")]
    [InlineData("my-namespace")]
    [InlineData("ns123")]
    public void IsValidNamespace_AcceptsLowercaseDashedNames(string ns)
    {
        Assert.True(ns.IsValidNamespace());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ab")]          // too short (< 3)
    [InlineData("-abc")]        // leading dash
    [InlineData("abc-")]        // trailing dash
    [InlineData("ab--c")]       // consecutive dash
    [InlineData("UpperCase")]   // uppercase not allowed
    [InlineData("with space")]
    public void IsValidNamespace_RejectsInvalidNames(string? ns)
    {
        Assert.False(ns!.IsValidNamespace());
    }

    #endregion

    #region Promotion code

    [Theory]
    [InlineData("abc12345")]
    [InlineData("promo2024code")]
    public void IsValidPromotionCode_AcceptsLowercaseAlphanumericOfMinLength(string code)
    {
        Assert.True(code.IsValidPromotionCode());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("abc123")]      // too short (< 8)
    [InlineData("ABC12345")]    // uppercase not allowed
    [InlineData("abc-1234")]    // dash not allowed
    public void IsValidPromotionCode_RejectsInvalidCodes(string? code)
    {
        Assert.False(code!.IsValidPromotionCode());
    }

    #endregion
}
