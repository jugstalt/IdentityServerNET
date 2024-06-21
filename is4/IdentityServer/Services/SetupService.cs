using IdentityServer.Nova;
using IdentityServer.Nova.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.Threading.Tasks;

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
            if(roleDb is not null)
            {
                TryCreateRole(roleDb, KnownRoles.UserAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.RoleAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.ResourceAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.ClientAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.SigningAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.SecretsVaultAdministrator).GetAwaiter().GetResult();
            }

            adminUser = new Nova.ApplicationUser()
            {
                UserName = "admin",
                Email = "admin@admin.com",
                EmailConfirmed = true,
                Roles = 
                    new string[] {
                        KnownRoles.UserAdministrator,
                        KnownRoles.RoleAdministrator,
                        KnownRoles.ResourceAdministrator,
                        KnownRoles.ClientAdministrator,
                        KnownRoles.SigningAdministrator,
                        KnownRoles.SecretsVaultAdministrator
                    }
            };

            var adminPassword = "admin";
            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

            userDb.CreateAsync(adminUser, CancellationToken.None).GetAwaiter().GetResult();
        }
    }

    async private Task<bool> TryCreateRole(IRoleDbContext roleDb,  string role)
    {
        try
        {
            var result = await roleDb.CreateAsync(
                    new ApplicationRole()
                    {
                        Name = role
                    }, 
                    CancellationToken.None
                );

            return result == IdentityResult.Success;
        } 
        catch { return false; }
    }
}
