using IdentityModel;
using IdentityServer.Nova.Models;

namespace IdentityServer.Nova.Services.PasswordHasher;

public class Sha512PasswordHasher : PasswordHasher
{
    override public string HashPassword(ApplicationUser user, string password)
    {
        return password.ToSha512();
    }
}
