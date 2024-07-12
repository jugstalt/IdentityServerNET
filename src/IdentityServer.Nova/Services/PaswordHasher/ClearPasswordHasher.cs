using IdentityServer.Nova.Models;

namespace IdentityServer.Nova.Services.PasswordHasher;

public class ClearPasswordHasher : PasswordHasher
{
    override public string HashPassword(ApplicationUser user, string password)
    {
        return password;
    }
}
