using IdentityModel;

namespace IdentityServer.Legacy.Services.PasswordHasher
{
    public class Sha256PasswordHasher : PasswordHasher
    {
        override public string HashPassword(ApplicationUser user, string password)
        {
            return password.ToSha256();
        }
    }
}
