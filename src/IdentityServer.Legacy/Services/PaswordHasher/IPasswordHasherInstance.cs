using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityServer.Legacy.Services.PaswordHasher
{
    public interface IPasswordHasherInstance : IPasswordHasher<ApplicationUser>
    {
    }
}
