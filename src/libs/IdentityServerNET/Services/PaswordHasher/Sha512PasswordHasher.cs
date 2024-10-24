using IdentityModel;
using IdentityServerNET.Models;

namespace IdentityServerNET.Services.PasswordHasher;

public class Sha512PasswordHasher : PasswordHasher
{
    override public string HashPassword(ApplicationUser user, string password)
    {
        return password.ToSha512();
    }
}
