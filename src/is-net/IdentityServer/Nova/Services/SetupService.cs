using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.EmailSender;
using IdentityServerNET.Abstractions.SigningCredential;
using IdentityServerNET.Models;
using IdentityServerNET.Services.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class SetupService
{
    private const string DefaultAdminLogin = "admin@is.net";

    public SetupService(
            IConfiguration config,
            ISigningCredentialCertificateStorage signingCredentialCertificateStorage,
            IUserDbContext userDb,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IRoleDbContext roleDb = null,
            IClientDbContext clientDb = null,
            IResourceDbContext resourceDb = null,
            ICustomEmailSender mailSender = null,
            DevMigrationService migration = null
        )
    {
        Console.WriteLine("################# Setup ##################");

        LogInstance(signingCredentialCertificateStorage);
        LogInstance(userDb);
        LogInstance(roleDb);
        LogInstance(clientDb);
        LogInstance(resourceDb);
        LogInstance(mailSender);

        var adminUser = userDb.FindByNameAsync(DefaultAdminLogin, CancellationToken.None).GetAwaiter().GetResult();

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
                UserName = DefaultAdminLogin,
                Email = DefaultAdminLogin,
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

            var adminPassword = migration?.AdminPassword ?? PasswordGenerator.GenerateSecurePassword(16);

            adminUser.PasswordHash = passwordHasher.HashPassword(adminUser, adminPassword);

            var result = userDb.CreateAsync(adminUser, CancellationToken.None).GetAwaiter().GetResult();

            if (result.Succeeded)
            {
                Console.WriteLine($"User {DefaultAdminLogin} created");
                Console.WriteLine($"Password: {adminPassword}");
            }
            else
            {
                Console.WriteLine("Can't create admin user:");
                Console.WriteLine(result.Errors.FirstOrDefault()?.Description);
            }
        }

        migration?.MigrateAsync().GetAwaiter().GetResult();

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
