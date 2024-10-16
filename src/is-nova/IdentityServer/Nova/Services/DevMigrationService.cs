#nullable enable

using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.PasswordHasher;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdParty.BouncyCastle.Asn1;

namespace IdentityServer.Nova.Services;

public class DevMigrationService
{
    private readonly MigrationModel _model;
    private readonly ILogger<DevMigrationService> _logger;
    private readonly IAdminUserDbContext? _userDb;
    private readonly IAdminRoleDbContext? _roleDb;
    private readonly IResourceDbContextModify? _resourceDb;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public DevMigrationService(
            IConfiguration config,
            ILogger<DevMigrationService> logger,
            IPasswordHasher<ApplicationUser> passwordHasher,
            IUserDbContext? userDb = null,
            IRoleDbContext? roleDb = null,
            IResourceDbContext? resourceDb = null)
    {
        _model = new MigrationModel();
        _logger = logger;

        config.Bind("IdentityServer:Migrations", _model);

        _userDb = userDb as IAdminUserDbContext;
        _roleDb = roleDb as IAdminRoleDbContext;
        _resourceDb = resourceDb as IResourceDbContextModify;

        _passwordHasher = passwordHasher;   
    }

    public string? AdminPassword
        => string.IsNullOrEmpty(_model.AdminPassword) 
            ? null 
            : _model.AdminPassword;

    async public Task<bool> MigrateAsync()
    {
        _logger.LogInformation("Run Dev Migrations");

        return
            await MigrateApiResourcesAsync() &&
            await MigrateIdentityResourcesAsync() && 
            await MigrateRolesAsync();
    }

    async public Task<bool> MigrateApiResourcesAsync()
    {
        if (_resourceDb is null) return false;

        foreach (var apiResouce in _model.ApiResources ?? [])
        {
            if (await _resourceDb.FindApiResourceAsync(apiResouce.Name) is null)
            {
                _logger.LogInformation("Migrate ApiResouce {apiResouce}", apiResouce.Name);

                List<ScopeModel> scopes = new List<ScopeModel>()
                {
                    new ScopeModel() { Name = apiResouce.Name }
                };

                if (apiResouce.Scopes?.Any() == true)
                {
                    scopes.AddRange(
                        apiResouce.Scopes.Select(scope => new ScopeModel()
                        {
                            Name = $"{apiResouce.Name}.{scope.Name}",
                        })
                    );
                }

                await _resourceDb.AddApiResourceAsync(new ApiResourceModel()
                {
                    Name = apiResouce.Name,
                    Scopes = scopes
                });
            }
        }

        return true;
    }

    async public Task<bool> MigrateIdentityResourcesAsync()
    {
        if (_resourceDb is null) return false;

        foreach (var identityResource in _model.IdentityResouces ?? [])
        {
            if (await _resourceDb.FindIdentityResource(identityResource.Name) is null)
            {
                _logger.LogInformation("Migrate IdentityResouce {identityResource}", identityResource.Name);

                await _resourceDb.AddIdentityResourceAsync(new IdentityResourceModel()
                {
                    Name = identityResource.Name
                });
            }
        }

        return true;
    }

    async public Task<bool> MigrateRolesAsync()
    {
        if(_roleDb is null) return false;

        foreach(var role in _model.Roles ?? [])
        {
            if (await _roleDb.FindByNameAsync(role.Name, CancellationToken.None) is null)
            {
                _logger.LogInformation("Migrate Role {role}", role.Name);

                await _roleDb.CreateAsync(new ApplicationRole() { Name = role.Name }, CancellationToken.None);
            }
        }

        return true;
    }

    async public Task<bool> MigrateUsersAsync()
    {
        if(_userDb is null) return false;   

        foreach (var user in _model.Users ?? [])
        {
            if (await _userDb.FindByNameAsync(user.Name, CancellationToken.None) is null)
            {
                _logger.LogInformation("Migrate User {user}", user.Name);

                var applicationUser = new ApplicationUser()
                {
                    UserName = user.Name,
                    Email = user.Name,
                    EmailConfirmed = true,
                    Roles = user.Roles
                };

                applicationUser.PasswordHash = _passwordHasher.HashPassword(applicationUser, "");

                var result = await _userDb.CreateAsync(applicationUser, CancellationToken.None);
            }
        }

        return true;
    }
}
