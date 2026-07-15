#nullable enable

using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;

namespace IdentityServerNET.Services.PasswordHasher;

public class Pbkdf2PasswordHasher : PasswordHasher
{
    // Self-describing format written for every new hash:
    // [FormatVersion(1)] [Iterations(4, big-endian)] [HashAlgorithmId(1)] [SaltSize(1)] [salt] [hash]
    // (HashSize is not stored - it's implicit: everything left over after the header and salt.)
    //
    // Embedding the parameters used to create a hash means Pbkdf2PasswordHasherOptions can be
    // changed later (e.g. a higher iteration count, to track OWASP's evolving recommendation)
    // without breaking verification of hashes created under the old settings - verification always
    // re-derives with whatever parameters are recorded in the hash itself, never with the hasher's
    // *current* options.
    private const byte FormatVersion = 0x01;
    private const int HeaderSize = 7; // FormatVersion(1) + Iterations(4) + HashAlgorithmId(1) + SaltSize(1)

    // Hashes written before this versioned format existed have no header at all - just
    // Base64(salt[32] || hash[64]), always PBKDF2-HMAC-SHA512 with 210,000 iterations. These
    // constants are intentionally frozen (independent of Pbkdf2PasswordHasherOptions' current
    // defaults) so pre-existing hashes stay readable regardless of future option changes; they are
    // never used to write new hashes. A bare (header-less) hash is unambiguously exactly
    // LegacyBareSaltSize + LegacyBareHashSize = 96 bytes long, which the versioned format above
    // never produces (its header alone adds 7 bytes), so the two formats are always distinguishable
    // by decoded length alone.
    private const int LegacyBareSaltSize = 32;
    private const int LegacyBareHashSize = 64;
    private const int LegacyBareIterations = 210_000;
    private static readonly HashAlgorithmName LegacyBareHashAlgorithmName = HashAlgorithmName.SHA512;

    private readonly Pbkdf2PasswordHasherOptions _options;
    private readonly string _template;

    public Pbkdf2PasswordHasher(
        IOptions<Pbkdf2PasswordHasherOptions>? options = null,
        IOptions<PasswordHashingOptions>? hashingOptions = null)
    {
        _options = options?.Value ?? new Pbkdf2PasswordHasherOptions();
        _template = hashingOptions?.Value?.Template ?? "{password}";
    }

    public override string HashPassword(ApplicationUser user, string password)
    {
        var input = _template.ApplyPasswordHashingTemplate(user, password);

        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            _options.Iterations,
            _options.HashAlgorithmName,
            _options.HashSize);

        var buffer = new byte[HeaderSize + _options.SaltSize + _options.HashSize];
        buffer[0] = FormatVersion;
        BinaryPrimitives.WriteInt32BigEndian(buffer.AsSpan(1, 4), _options.Iterations);
        buffer[5] = AlgorithmToByte(_options.HashAlgorithmName);
        buffer[6] = checked((byte)_options.SaltSize); // SaltSize must fit in a single byte (<= 255)
        Array.Copy(salt, 0, buffer, HeaderSize, _options.SaltSize);
        Array.Copy(hash, 0, buffer, HeaderSize + _options.SaltSize, _options.HashSize);

        return Convert.ToBase64String(buffer);
    }

    public override PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        byte[] hashBytes;
        try
        {
            hashBytes = Convert.FromBase64String(hashedPassword);
        }
        catch (FormatException)
        {
            return PasswordVerificationResult.Failed;
        }

        return hashBytes.Length == LegacyBareSaltSize + LegacyBareHashSize
            ? VerifyLegacyBareFormat(user, hashBytes, providedPassword)
            : VerifyVersionedFormat(user, hashBytes, providedPassword);
    }

    private PasswordVerificationResult VerifyVersionedFormat(ApplicationUser user, byte[] hashBytes, string providedPassword)
    {
        if (hashBytes.Length < HeaderSize || hashBytes[0] != FormatVersion)
        {
            return PasswordVerificationResult.Failed;
        }

        var iterations = BinaryPrimitives.ReadInt32BigEndian(hashBytes.AsSpan(1, 4));

        HashAlgorithmName algorithm;
        try
        {
            algorithm = ByteToAlgorithm(hashBytes[5]);
        }
        catch (ArgumentOutOfRangeException)
        {
            return PasswordVerificationResult.Failed;
        }

        int saltSize = hashBytes[6];
        int hashSize = hashBytes.Length - HeaderSize - saltSize;

        if (iterations <= 0 || saltSize <= 0 || hashSize <= 0)
        {
            return PasswordVerificationResult.Failed;
        }

        var salt = hashBytes.AsSpan(HeaderSize, saltSize);
        var storedHash = hashBytes.AsSpan(HeaderSize + saltSize, hashSize);

        var input = _template.ApplyPasswordHashingTemplate(user, providedPassword);
        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(input, salt, iterations, algorithm, hashSize);

        return CryptographicOperations.FixedTimeEquals(computedHash, storedHash)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }

    private PasswordVerificationResult VerifyLegacyBareFormat(ApplicationUser user, byte[] hashBytes, string providedPassword)
    {
        var salt = hashBytes.AsSpan(0, LegacyBareSaltSize);
        var storedHash = hashBytes.AsSpan(LegacyBareSaltSize, LegacyBareHashSize);

        var input = _template.ApplyPasswordHashingTemplate(user, providedPassword);
        byte[] computedHash = Rfc2898DeriveBytes.Pbkdf2(
            input, salt, LegacyBareIterations, LegacyBareHashAlgorithmName, LegacyBareHashSize);

        if (!CryptographicOperations.FixedTimeEquals(computedHash, storedHash))
        {
            return PasswordVerificationResult.Failed;
        }

        // Correct, but in the old header-less format - upgrade to the versioned format on next
        // login via the same rehash mechanism already used for the legacy SHA hashers.
        return PasswordVerificationResult.SuccessRehashNeeded;
    }

    private static byte AlgorithmToByte(HashAlgorithmName algorithm)
    {
        if (algorithm == HashAlgorithmName.SHA1) return 0;
        if (algorithm == HashAlgorithmName.SHA256) return 1;
        if (algorithm == HashAlgorithmName.SHA384) return 2;
        if (algorithm == HashAlgorithmName.SHA512) return 3;

        throw new NotSupportedException(
            $"Hash algorithm '{algorithm.Name}' is not supported by the versioned PBKDF2 format.");
    }

    private static HashAlgorithmName ByteToAlgorithm(byte value) => value switch
    {
        0 => HashAlgorithmName.SHA1,
        1 => HashAlgorithmName.SHA256,
        2 => HashAlgorithmName.SHA384,
        3 => HashAlgorithmName.SHA512,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown hash algorithm id.")
    };
}
