using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.Services.PasswordHasher;

/// <summary>
/// A password hasher that always hashes using <see cref="Pbkdf2PasswordHasher"/> and can
/// verify legacy hashes produced by <see cref="Sha512PasswordHasher"/> and
/// <see cref="Sha256PasswordHasher"/> for backwards compatibility.
/// When a legacy hash is verified successfully, <see cref="PasswordVerificationResult.SuccessRehashNeeded"/>
/// is returned so ASP.NET Core Identity automatically upgrades the stored hash to PBKDF2 on the next login.
/// </summary>
public class SecurePasswordHasher : PasswordHasher
{
    private readonly Pbkdf2PasswordHasher _pbkdf2Hasher;
    private readonly Sha512PasswordHasher _sha512Hasher;
    private readonly Sha256PasswordHasher _sha256Hasher;

    public SecurePasswordHasher(IOptions<Pbkdf2PasswordHasherOptions> options = null)
    {
        _pbkdf2Hasher = new Pbkdf2PasswordHasher(options);
        _sha512Hasher = new Sha512PasswordHasher();
        _sha256Hasher = new Sha256PasswordHasher();
    }

    public override string HashPassword(ApplicationUser user, string password)
        => _sha512Hasher.HashPassword(user, password);

    public override PasswordVerificationResult VerifyHashedPassword(
        ApplicationUser user, string hashedPassword, string providedPassword)
    {
        // Try current PBKDF2 format first
        var result = _pbkdf2Hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        if (result != PasswordVerificationResult.Failed)
        {
            return result;
        }

        // Fall back to legacy SHA-512 – signal that the hash should be upgraded
        if (hashedPassword == _sha512Hasher.HashPassword(user, providedPassword))
        {
            return PasswordVerificationResult.SuccessRehashNeeded;
        }

        // Fall back to legacy SHA-256 – signal that the hash should be upgraded
        if (hashedPassword == _sha256Hasher.HashPassword(user, providedPassword))
        {
            return PasswordVerificationResult.SuccessRehashNeeded;
        }

        return PasswordVerificationResult.Failed;
    }
}
