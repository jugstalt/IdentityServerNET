using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Nova.Services.PaswordHasher;

public interface IPasswordHasherInstance : IPasswordHasher<ApplicationUser>
{
}
