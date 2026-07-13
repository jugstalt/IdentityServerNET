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
    private readonly IClientDbContext _clientDb;
    private readonly IResourceDbContext _resourceDb;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public RealmProvisioningService(
        IRealmDbContext realmDb,
        IRoleDbContext roleDb,
        IUserDbContext userDb,
        IClientDbContext clientDb,
        IResourceDbContext resourceDb,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _realmDb = realmDb;
        _roleDb = roleDb;
        _userDb = userDb;
        _clientDb = clientDb;
        _resourceDb = resourceDb;
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
            Roles = realmRoleNames,
            MustChangePassword = true
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

    public async Task DeleteRealmAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var stored = await _realmDb.FindByNameAsync(realm.Name, cancellationToken);
        if (stored is null)
        {
            return;
        }

        // Act as the realm being removed, so the store decorators permit deleting its realm-scoped
        // items from the system-admin context.
        using (RealmScopeOverride.Begin(stored.Name))
        {
            if (_clientDb is IClientDbContextModify clientMod)
            {
                foreach (var client in (await clientMod.GetAllClients()).ToArray())
                {
                    await clientMod.RemoveClientAsync(client);
                }
            }

            if (_resourceDb is IResourceDbContextModify resourceMod)
            {
                foreach (var api in (await resourceMod.GetAllApiResources())
                            .Where(r => r.Name.BelongsToRealm(stored.Name)).ToArray())
                {
                    await resourceMod.RemoveApiResourceAsync(api);
                }

                foreach (var identity in (await resourceMod.GetAllIdentityResources())
                            .Where(r => r.Name.BelongsToRealm(stored.Name)).ToArray())
                {
                    await resourceMod.RemoveIdentityResourceAsync(identity);
                }
            }

            if (_roleDb is IAdminRoleDbContext adminRoles)
            {
                foreach (var role in (await adminRoles.GetRolesAsync(int.MaxValue, 0, cancellationToken)).ToArray())
                {
                    await _roleDb.DeleteAsync(role, cancellationToken);
                }
            }

            var adminUser = await _userDb.FindByNameAsync($"admin@{stored.PrimaryDomain}", cancellationToken);
            if (adminUser is not null)
            {
                await _userDb.DeleteAsync(adminUser, cancellationToken);
            }
        }

        await _realmDb.DeleteAsync(stored, cancellationToken);
    }
}
