namespace IdentityServer.Legacy.Services.PasswordHasher
{
    public class ClearPasswordHasher : PasswordHasher
    {
        override public string HashPassword(ApplicationUser user, string password)
        {
            return password;
        }
    }
}
