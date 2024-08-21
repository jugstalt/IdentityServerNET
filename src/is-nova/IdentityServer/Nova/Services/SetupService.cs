using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.SigningCredential;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services;

public class SetupService
{

    public SetupService(
            IConfiguration config,
            ISigningCredentialCertificateStorage signingCredentialCertificateStorage,
            IUserDbContext userDb,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRoleDbContext roleDb = null,
            IClientDbContext clientDb = null,
            IResourceDbContext resourceDb = null
        )
    {
        Console.WriteLine("################# Setup ##################");

        LogInstance(signingCredentialCertificateStorage);
        LogInstance(userDb);
        LogInstance(roleDb);
        LogInstance(clientDb);
        LogInstance(resourceDb);

        var adminUser = userDb.FindByNameAsync("admin", CancellationToken.None).GetAwaiter().GetResult();

        if (adminUser is null)
        {
            if (roleDb is not null)
            {
                foreach (var methodInfo in typeof(KnownRoles).GetMethods().Where(m => m.ReturnType == typeof(ApplicationRole)))
                {
                    var knownRole = (ApplicationRole)methodInfo.Invoke(Activator.CreateInstance<KnownRoles>(), null);

                    TryCreateRole(roleDb, knownRole).GetAwaiter().GetResult();
                }
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

    async private Task<bool> TryCreateRole(IRoleDbContext roleDb, ApplicationRole role)
    {
        try
        {
            var result = await roleDb.CreateAsync(
                    new ApplicationRole()
                    {
                        Name = role.Name,
                        Description = role.Description,
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
