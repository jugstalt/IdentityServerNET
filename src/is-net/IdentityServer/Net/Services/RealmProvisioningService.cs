using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Cryptography;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class RealmProvisioningService : IRealmProvisioningService
{
    private readonly IRealmDbContext _realmDb;
    private readonly IRoleDbContext _roleDb;
    private readonly IUserDbContext _userDb;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public RealmProvisioningService(
        IRealmDbContext realmDb,
        IRoleDbContext roleDb,
        IUserDbContext userDb,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _realmDb = realmDb;
        _roleDb = roleDb;
        _userDb = userDb;
        _passwordHasher = passwordHasher;
    }

    public async Task<RealmProvisioningResult> CreateRealmAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();

        // Stores the realm and enforces the "a domain belongs to at most one realm" invariant.
        await _realmDb.CreateAsync(realm, cancellationToken);

        // Provision the realm-scoped administrator roles (role@realm). Runs in the system context, so
        // the role decorator keeps the explicit suffix.
        var realmRoleNames = KnownRoles.RealmDelegatedRoles
            .Select(role => role.AddRealmNamespace(realm.Name))
            .ToArray();

        foreach (var roleName in realmRoleNames)
        {
            await _roleDb.CreateAsync(
                new ApplicationRole
                {
                    Id = roleName,
                    Name = roleName,
                    Description = $"Realm '{realm.Name}' administrator role",
                    CreateDate = DateTime.UtcNow
                },
                cancellationToken);
        }

        // Provision the realm admin user, anchored to the primary domain.
        var adminUserName = $"admin@{realm.PrimaryDomain}";

        if (await _userDb.FindByNameAsync(adminUserName, cancellationToken) != null)
        {
            throw new InvalidOperationException($"User '{adminUserName}' already exists.");
        }

        var password = PasswordGenerator.GenerateSecurePassword(16);

        var adminUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = adminUserName,
            Email = adminUserName,
            EmailConfirmed = true,
            Roles = realmRoleNames
        };
        adminUser.PasswordHash = _passwordHasher.HashPassword(adminUser, password);

        var result = await _userDb.CreateAsync(adminUser, cancellationToken);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Can't create realm admin '{adminUserName}': {result.Errors?.FirstOrDefault()?.Description}");
        }

        return new RealmProvisioningResult
        {
            RealmName = realm.Name,
            AdminUserName = adminUserName,
            AdminPassword = password
        };
    }
}
