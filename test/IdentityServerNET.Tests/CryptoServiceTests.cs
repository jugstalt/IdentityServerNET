using System.Text;
using IdentityServerNET.Services.Cryptography;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for the symmetric crypto services used to protect secrets at rest.
/// They verify correct round-tripping, that encryption is randomized (IND-CPA friendly)
/// and that weak configuration is rejected.
/// </summary>
public class CryptoServiceTests
{
    private const string ValidPassword = "0123456789-abcdefghijklmnopqrstuv"; // >= 24 bytes

    private static DefaultCryptoService CreateDefaultService(string? password = null) =>
        new DefaultCryptoService(Options.Create(new DefaultCryptoServiceOptions
        {
            Password = password ?? ValidPassword
        }));

    #region DefaultCryptoService

    [Fact]
    public void Default_EncryptThenDecrypt_RoundTripsToOriginalText()
    {
        var service = CreateDefaultService();
        const string secret = "Top secret value äöü 123";

        var cipher = service.EncryptText(secret);
        var plain = service.DecryptText(cipher);

        Assert.Equal(secret, plain);
    }

    [Fact]
    public void Default_EncryptText_DoesNotLeakPlainText()
    {
        var service = CreateDefaultService();
        const string secret = "super-secret";

        var cipher = service.EncryptText(secret);

        Assert.NotEqual(secret, cipher);
        Assert.DoesNotContain(secret, cipher);
    }

    [Fact]
    public void Default_EncryptText_IsRandomized_SamePlainTextProducesDifferentCipherText()
    {
        var service = CreateDefaultService();
        const string secret = "same-input";

        var cipher1 = service.EncryptText(secret);
        var cipher2 = service.EncryptText(secret);

        Assert.NotEqual(cipher1, cipher2);
        Assert.Equal(secret, service.DecryptText(cipher1));
        Assert.Equal(secret, service.DecryptText(cipher2));
    }

    [Fact]
    public void Default_PseudoHashTextConvergent_IsDeterministic()
    {
        var service = CreateDefaultService();
        const string input = "user@example.com";

        var hash1 = service.PseudoHashTextConvergent(input);
        var hash2 = service.PseudoHashTextConvergent(input);

        Assert.Equal(hash1, hash2);
        Assert.NotEqual(input, hash1);
    }

    [Fact]
    public void Default_EmptyInput_ReturnsEmptyString()
    {
        var service = CreateDefaultService();

        Assert.Equal(string.Empty, service.EncryptText(string.Empty));
    }

    [Theory]
    [InlineData("short")]
    [InlineData("0123456789abcdef")] // 16 bytes, still below the 24 byte minimum
    public void Default_Constructor_WithTooShortPassword_Throws(string weakPassword)
    {
        Assert.ThrowsAny<System.Exception>(() => CreateDefaultService(weakPassword));
    }

    #endregion

    #region Base64CryptoService

    [Fact]
    public void Base64_EncryptThenDecrypt_RoundTrips()
    {
        var service = new Base64CryptoService();
        const string text = "round-trip-me";

        var encoded = service.EncryptText(text);
        var decoded = service.DecryptText(encoded);

        Assert.Equal(text, decoded);
        Assert.NotEqual(text, encoded);
    }

    [Fact]
    public void Base64_PseudoHashTextConvergent_EqualsEncryptText()
    {
        var service = new Base64CryptoService();
        const string text = "convergent";

        Assert.Equal(service.EncryptText(text), service.PseudoHashTextConvergent(text));
    }

    [Fact]
    public void Base64_RespectsExplicitEncoding()
    {
        var service = new Base64CryptoService();
        const string text = "äöü";

        var encoded = service.EncryptText(text, Encoding.UTF8);
        var decoded = service.DecryptText(encoded, Encoding.UTF8);

        Assert.Equal(text, decoded);
    }

    #endregion

    #region ClearTextCryptoService

    [Fact]
    public void ClearText_IsPassthrough_DocumentsInsecureDevOnlyBehavior()
    {
        // ClearTextCryptoService performs no encryption and is for development only.
        var service = new ClearTextCryptoService();
        const string text = "not-encrypted";

        Assert.Equal(text, service.EncryptText(text));
        Assert.Equal(text, service.DecryptText(text));
        Assert.Equal(text, service.PseudoHashTextConvergent(text));
    }

    #endregion
}
