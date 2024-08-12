using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Nova.Services.PasswordHasher;

abstract public class PasswordHasher : IPasswordHasher<ApplicationUser>
{
    abstract public string HashPassword(ApplicationUser user, string password);

    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        if (user.PasswordHash == HashPassword(user, providedPassword))
        {
            return PasswordVerificationResult.Success;
        }

        return PasswordVerificationResult.Failed;
    }
}
