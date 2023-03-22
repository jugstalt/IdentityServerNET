using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Legacy.Services.PaswordHasher
{
    public interface IPasswordHasherInstance : IPasswordHasher<ApplicationUser>
    {
    }
}
