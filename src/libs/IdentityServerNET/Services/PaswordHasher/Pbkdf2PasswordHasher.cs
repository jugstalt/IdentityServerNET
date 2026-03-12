#nullable enable

using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;

namespace IdentityServerNET.Services.PasswordHasher;

public class Pbkdf2PasswordHasher : PasswordHasher
{
    private readonly Pbkdf2PasswordHasherOptions _options;

    public Pbkdf2PasswordHasher(IOptions<Pbkdf2PasswordHasherOptions>? options = null)
    {
        _options = options?.Value ?? new Pbkdf2PasswordHasherOptions();
    }

    public override string HashPassword(ApplicationUser user, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_options.SaltSize);

        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            _options.Iterations,
            _options.HashAlgorithmName,
            _options.HashSize);

        // Format: [salt (SaltSize bytes) | hash (HashSize bytes)]
        byte[] hashBytes = new byte[_options.SaltSize + _options.HashSize];
        Array.Copy(salt, 0, hashBytes, 0, _options.SaltSize);
        Array.Copy(hash, 0, hashBytes, _options.SaltSize, _options.HashSize);

        return Convert.ToBase64String(hashBytes);
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

        if (hashBytes.Length != _options.SaltSize + _options.HashSize)
        {
            return PasswordVerificationResult.Failed;
        }

        // Extract the salt from the first SaltSize bytes
        byte[] salt = new byte[_options.SaltSize];
        Array.Copy(hashBytes, 0, salt, 0, _options.SaltSize);

        // Re-hash the provided password with the extracted salt
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            providedPassword,
            salt,
            _options.Iterations,
            _options.HashAlgorithmName,
            _options.HashSize);

        // Timing-safe comparison to prevent timing attacks
        byte[] storedHash = new byte[_options.HashSize];
        Array.Copy(hashBytes, _options.SaltSize, storedHash, 0, _options.HashSize);

        return CryptographicOperations.FixedTimeEquals(hash, storedHash)
            ? PasswordVerificationResult.Success
            : PasswordVerificationResult.Failed;
    }
}