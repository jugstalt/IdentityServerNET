using IdentityServer.Nova;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services;

public class SetupService
{

    public SetupService(
            IConfiguration config,
            IUserDbContext userDb,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRoleDbContext roleDb = null,
            IClientDbContext clientDb = null,
            IResourceDbContext resourceDb = null
        )
    {
        Console.WriteLine("################# Setup ##################");

        LogInstance(userDb);
        LogInstance(roleDb);
        LogInstance(clientDb);
        LogInstance(resourceDb);

        var adminUser = userDb.FindByNameAsync("admin", CancellationToken.None).GetAwaiter().GetResult();

        if (adminUser is null)
        {
            if (roleDb is not null)
            {
                TryCreateRole(roleDb, KnownRoles.UserAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.RoleAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.ResourceAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.ClientAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.SigningAdministrator).GetAwaiter().GetResult();
                TryCreateRole(roleDb, KnownRoles.SecretsVaultAdministrator).GetAwaiter().GetResult();
            }

            adminUser = new ApplicationUser()
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

            var adminPassword = PasswordGenerator.GenerateSecurePassword(16);

            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

            userDb.CreateAsync(adminUser, CancellationToken.None).GetAwaiter().GetResult();

            Console.WriteLine("User admin created");
            Console.WriteLine($"Password: {adminPassword}");
        }
        Console.WriteLine("#########################################");
    }

    async private Task<bool> TryCreateRole(IRoleDbContext roleDb, string role)
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

    private void LogInstance<T>(T instance)
    {
        Console.WriteLine($"{typeof(T).Name}: {(instance is null ? "not registered" : instance.GetType())}");
    }
}
