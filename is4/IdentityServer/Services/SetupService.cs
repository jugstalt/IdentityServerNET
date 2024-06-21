using IdentityServer.Nova;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace IdentityServer.Services;

public class SetupService
{
    public SetupService(
            IConfiguration config,
            IUserDbContext userDb,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRoleDbContext roleDb = null
        )
    {
        var adminUser = userDb.FindByNameAsync("admin", CancellationToken.None).GetAwaiter().GetResult();
        if (adminUser is null)
        {
            adminUser = new Nova.ApplicationUser()
            {
                UserName = "admin",
                Email = "admin@admin.com",
                EmailConfirmed = true
            };

            var adminPassword = "admin";
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

            userDb.CreateAsync(adminUser, CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
