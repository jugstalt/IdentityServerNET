using System.Security.Cryptography;

namespace IdentityServerNET.Services.PasswordHasher;

public class Pbkdf2PasswordHasherOptions
{
    public Pbkdf2PasswordHasherOptions()
    {
        // 32 bytes (256 bit): OWASP minimum recommendation for PBKDF2 salts.
        // A random salt of this length prevents rainbow-table attacks and ensures
        // that identical passwords produce different hashes.
        SaltSize = 32;

        // 64 bytes (512 bit): matches the native output length of SHA-512.
        // Truncating the output would not introduce a vulnerability, but would
        // waste CPU cycles – so the full native output length is used.
        HashSize = 64;

        // 210,000 iterations: OWASP 2023 minimum recommendation for PBKDF2-SHA512.
        // Higher iteration counts increase the cost for attackers linearly, but also
        // slow down legitimate logins. Revisit this value periodically as hardware
        // improves (e.g. double it every 2 years).
        // For reference: ASP.NET Core Identity uses 350,000 iterations (SHA-512) since .NET 8.
        Iterations = 210_000;

        // SHA-512: recommended by OWASP for PBKDF2. On 64-bit systems SHA-512 is faster
        // than SHA-256, so fewer iterations are needed for equivalent security.
        // SHA-1 and MD5 are deprecated and must not be used.
        HashAlgorithmName = HashAlgorithmName.SHA512;
    }

    /// <summary>
    /// Size of the random salt in bytes. Default: 32 (256 bit).
    /// </summary>
    public int SaltSize { get; set; }

    /// <summary>
    /// Size of the derived hash in bytes. Default: 64 (512 bit, matches SHA-512 native output).
    /// </summary>
    public int HashSize { get; set; }

    /// <summary>
    /// Number of PBKDF2 iterations. Default: 210,000 (OWASP 2023 minimum for PBKDF2-SHA512).
    /// </summary>
    public int Iterations { get; set; }

    /// <summary>
    /// Hash algorithm used for PBKDF2. Default: SHA-512.
    /// </summary>
    public HashAlgorithmName HashAlgorithmName { get; set; }
}
