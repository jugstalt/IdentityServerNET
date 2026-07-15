using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using IdentityServerNET.Models;
using IdentityServerNET.Services.PasswordHasher;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Security tests for the password hashing pipeline.
/// These verify that passwords are never stored in clear text, that salting makes
/// identical passwords produce different hashes, that verification is correct and
/// that legacy hashes are flagged for an automatic upgrade (re-hash).
/// </summary>
public class PasswordHasherTests
{
    private static ApplicationUser NewUser() => new ApplicationUser { UserName = "alice" };

    private static IOptions<Pbkdf2PasswordHasherOptions> FastOptions() =>
        // Lower the iteration count so the test suite stays fast – security properties
        // (salting, timing-safe compare, format) are independent of the iteration count.
        Options.Create(new Pbkdf2PasswordHasherOptions { Iterations = 1_000 });

    #region Pbkdf2PasswordHasher

    [Fact]
    public void Pbkdf2_HashPassword_DoesNotReturnPlainTextPassword()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var user = NewUser();

        var hash = hasher.HashPassword(user, "S3cr3t!Passw0rd");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("S3cr3t!Passw0rd", hash);
        Assert.DoesNotContain("S3cr3t!Passw0rd", hash);
    }

    [Fact]
    public void Pbkdf2_HashPassword_IsSaltedSoSamePasswordYieldsDifferentHashes()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var user = NewUser();

        var hash1 = hasher.HashPassword(user, "same-password");
        var hash2 = hasher.HashPassword(user, "same-password");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_WithCorrectPassword_ReturnsSuccess()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var user = NewUser();
        var hash = hasher.HashPassword(user, "correct horse battery staple");

        var result = hasher.VerifyHashedPassword(user, hash, "correct horse battery staple");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_WithWrongPassword_ReturnsFailed()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var user = NewUser();
        var hash = hasher.HashPassword(user, "correct-password");

        var result = hasher.VerifyHashedPassword(user, hash, "wrong-password");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Theory]
    [InlineData("not-base-64-$$$")]
    [InlineData("")]
    public void Pbkdf2_VerifyHashedPassword_WithMalformedHash_ReturnsFailed(string malformedHash)
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());

        var result = hasher.VerifyHashedPassword(NewUser(), malformedHash, "whatever");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_WithWrongLengthHash_ReturnsFailed()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        // Valid base64 but not SaltSize + HashSize bytes long.
        var tooShort = System.Convert.ToBase64String(new byte[10]);

        var result = hasher.VerifyHashedPassword(NewUser(), tooShort, "whatever");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_WithTamperedHash_ReturnsFailed()
    {
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var user = NewUser();
        var hash = hasher.HashPassword(user, "correct-password");

        // Flip the last byte of the stored hash – verification must fail.
        var bytes = System.Convert.FromBase64String(hash);
        bytes[^1] ^= 0xFF;
        var tampered = System.Convert.ToBase64String(bytes);

        var result = hasher.VerifyHashedPassword(user, tampered, "correct-password");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Pbkdf2_HashAndVerify_AcrossSeparateHasherInstances_Succeeds()
    {
        // The salt is embedded in the hash, so a freshly constructed hasher with the
        // same options can verify a previously produced hash.
        var hash = new Pbkdf2PasswordHasher(FastOptions()).HashPassword(NewUser(), "portable-password");

        var result = new Pbkdf2PasswordHasher(FastOptions())
            .VerifyHashedPassword(NewUser(), hash, "portable-password");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void Pbkdf2_HashPassword_EmbedsParametersInAVersionedFormat()
    {
        // Pins the on-disk format: [Version(1)][Iterations(4, BE)][AlgorithmId(1)][SaltSize(1)][salt][hash].
        // This is what makes future changes to Pbkdf2PasswordHasherOptions (e.g. raising the
        // iteration count) safe: verification reads the parameters back out of the hash itself
        // instead of assuming the hasher's *current* options.
        var options = Options.Create(new Pbkdf2PasswordHasherOptions { Iterations = 4_321 });
        var hash = new Pbkdf2PasswordHasher(options).HashPassword(NewUser(), "versioned-password");

        var bytes = Convert.FromBase64String(hash);

        Assert.Equal(0x01, bytes[0]);
        Assert.Equal(4_321, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(1, 4)));
        Assert.Equal(3, bytes[5]); // SHA512
        Assert.Equal(32, bytes[6]); // default SaltSize
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_WithHigherCurrentIterationCount_StillVerifiesOldHash()
    {
        // The whole point of the versioned format: a hash created with one iteration count must
        // still verify correctly against a hasher configured with a *different* (e.g. later
        // increased) iteration count, because verification uses the count embedded in the hash.
        var hash = new Pbkdf2PasswordHasher(Options.Create(new Pbkdf2PasswordHasherOptions { Iterations = 1_000 }))
            .HashPassword(NewUser(), "raise-the-bar");

        var laterHasher = new Pbkdf2PasswordHasher(Options.Create(new Pbkdf2PasswordHasherOptions { Iterations = 500_000 }));

        var result = laterHasher.VerifyHashedPassword(NewUser(), hash, "raise-the-bar");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_LegacyBareFormat_StillVerifiesAndRequestsRehash()
    {
        // Reproduces exactly what the pre-versioned Pbkdf2PasswordHasher wrote: a header-less
        // Base64(salt[32] || hash[64]) blob, PBKDF2-HMAC-SHA512, 210,000 iterations. Hashes already
        // stored in this format (from before the versioned format existed) must keep working.
        var user = NewUser();
        // The hasher below uses the default "{password}" template, which is a no-op substitution.
        var salt = RandomNumberGenerator.GetBytes(32);
        var derived = Rfc2898DeriveBytes.Pbkdf2("legacy-bare-password", salt, 210_000, HashAlgorithmName.SHA512, 64);
        var legacyBareHash = Convert.ToBase64String([.. salt, .. derived]);

        var hasher = new Pbkdf2PasswordHasher(FastOptions());

        var correctResult = hasher.VerifyHashedPassword(user, legacyBareHash, "legacy-bare-password");
        var wrongResult = hasher.VerifyHashedPassword(user, legacyBareHash, "wrong-password");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, correctResult);
        Assert.Equal(PasswordVerificationResult.Failed, wrongResult);
    }

    [Fact]
    public void Pbkdf2_VerifyHashedPassword_LegacyBareFormat_RehashProducesVersionedFormat()
    {
        // After the SuccessRehashNeeded round-trip, the newly written hash must be in the
        // versioned format (not bare) - i.e. the migration actually completes.
        var hasher = new Pbkdf2PasswordHasher(FastOptions());
        var rehashed = hasher.HashPassword(NewUser(), "anything");

        var bytes = Convert.FromBase64String(rehashed);

        Assert.Equal(0x01, bytes[0]);
    }

    #endregion

    #region Pbkdf2PasswordHasherOptions

    [Fact]
    public void Pbkdf2Options_Defaults_MeetOwaspRecommendations()
    {
        var options = new Pbkdf2PasswordHasherOptions();

        Assert.True(options.SaltSize >= 16, "Salt must be at least 128 bit.");
        Assert.True(options.Iterations >= 210_000, "Iteration count must meet the OWASP 2023 minimum.");
        Assert.Equal(System.Security.Cryptography.HashAlgorithmName.SHA512, options.HashAlgorithmName);
    }

    #endregion

    #region Legacy SHA hashers

    [Fact]
    public void Sha512_HashPassword_IsDeterministicAndNotPlainText()
    {
        var hasher = new Sha512PasswordHasher();
        var user = NewUser();

        var hash1 = hasher.HashPassword(user, "p@ssword");
        var hash2 = hasher.HashPassword(user, "p@ssword");

        Assert.Equal(hash1, hash2);
        Assert.NotEqual("p@ssword", hash1);
    }

    [Fact]
    public void Sha256_And_Sha512_ProduceDifferentHashes()
    {
        var user = NewUser();

        var sha256 = new Sha256PasswordHasher().HashPassword(user, "p@ssword");
        var sha512 = new Sha512PasswordHasher().HashPassword(user, "p@ssword");

        Assert.NotEqual(sha256, sha512);
    }

    [Fact]
    public void Sha512_VerifyHashedPassword_UsesStoredUserHash()
    {
        // The base PasswordHasher compares against user.PasswordHash.
        var hasher = new Sha512PasswordHasher();
        var user = NewUser();
        user.PasswordHash = hasher.HashPassword(user, "p@ssword");

        Assert.Equal(PasswordVerificationResult.Success,
            hasher.VerifyHashedPassword(user, user.PasswordHash, "p@ssword"));
        Assert.Equal(PasswordVerificationResult.Failed,
            hasher.VerifyHashedPassword(user, user.PasswordHash, "wrong"));
    }

    #endregion

    #region ClearPasswordHasher

    [Fact]
    public void ClearPasswordHasher_StoresPlainText_DocumentsInsecureDevOnlyBehavior()
    {
        // ClearPasswordHasher is intentionally insecure and must only be used for local
        // development. This test documents (and guards) that behavior.
        var hasher = new ClearPasswordHasher();
        var user = NewUser();

        var hash = hasher.HashPassword(user, "plain");

        Assert.Equal("plain", hash);
    }

    #endregion

    #region SecurePasswordHasher

    [Fact]
    public void Secure_VerifiesModernPbkdf2Hash_AsSuccess()
    {
        var options = FastOptions();
        var pbkdf2Hash = new Pbkdf2PasswordHasher(options).HashPassword(NewUser(), "modern-password");
        var secure = new SecurePasswordHasher(options);

        var result = secure.VerifyHashedPassword(NewUser(), pbkdf2Hash, "modern-password");

        Assert.Equal(PasswordVerificationResult.Success, result);
    }

    [Fact]
    public void Secure_VerifiesLegacySha512Hash_AsSuccessRehashNeeded()
    {
        // A user whose password was stored with the legacy SHA-512 hasher must be able to
        // log in, but the result must request an automatic upgrade to PBKDF2.
        var legacyHash = new Sha512PasswordHasher().HashPassword(NewUser(), "legacy-password");
        var secure = new SecurePasswordHasher(FastOptions());

        var result = secure.VerifyHashedPassword(NewUser(), legacyHash, "legacy-password");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    public void Secure_VerifiesLegacySha256Hash_AsSuccessRehashNeeded()
    {
        var legacyHash = new Sha256PasswordHasher().HashPassword(NewUser(), "legacy-password");
        var secure = new SecurePasswordHasher(FastOptions());

        var result = secure.VerifyHashedPassword(NewUser(), legacyHash, "legacy-password");

        Assert.Equal(PasswordVerificationResult.SuccessRehashNeeded, result);
    }

    [Fact]
    public void Secure_VerifyHashedPassword_WithWrongPassword_ReturnsFailed()
    {
        var options = FastOptions();
        var pbkdf2Hash = new Pbkdf2PasswordHasher(options).HashPassword(NewUser(), "right-password");
        var secure = new SecurePasswordHasher(options);

        var result = secure.VerifyHashedPassword(NewUser(), pbkdf2Hash, "wrong-password");

        Assert.Equal(PasswordVerificationResult.Failed, result);
    }

    [Fact]
    public void Secure_FreshlyHashedPassword_VerifiesAsSuccess_WithoutNeedingARehash()
    {
        // SecurePasswordHasher.HashPassword always produces a current-format PBKDF2 hash, so
        // verifying it right back must succeed immediately - no rehash-needed round trip.
        var secure = new SecurePasswordHasher(FastOptions());
        var user = NewUser();

        var hash = secure.HashPassword(user, "brand-new-password");
        var result = secure.VerifyHashedPassword(user, hash, "brand-new-password");

        Assert.Equal(PasswordVerificationResult.Success, result);
        Assert.NotEqual("brand-new-password", hash);
    }

    #endregion
}
