using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class InMemoryRoleDb : IRoleDbContext, IAdminRoleDbContext
{
    private static ConcurrentDictionary<string, ApplicationRole> _roles = new ConcurrentDictionary<string, ApplicationRole>();

    #region 

    async public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        if (await FindByIdAsync(role.Id, cancellationToken) != null &&
            await FindByNameAsync(role.Name, cancellationToken) != null)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "already_exists",
                Description = "Role already exists"
            });
        }

        if (String.IsNullOrWhiteSpace(role.Id))
        {
            role.Id = Guid.NewGuid().ToString().ToLower();
        }

        _roles.TryAdd(role.Id, role);

        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        if (!_roles.ContainsKey(role.Id))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError()
            {
                Code = "not_exists",
                Description = "Role not exists"
            }));
        }

        _roles.TryRemove(role.Id, out role);

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<ApplicationRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        if (!_roles.ContainsKey(roleId))
        {
            return Task.FromResult<ApplicationRole>(null);
        }

        return Task.FromResult(_roles[roleId]);
    }

    public Task<ApplicationRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        normalizedRoleName = normalizedRoleName.ToUpper();
        var role = _roles.Values
            .ToArray()
            .Where(u => u.Name.ToUpper() == normalizedRoleName)
            .FirstOrDefault();

        return Task.FromResult(role);
    }

    async public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        var storedRole = await FindByIdAsync(role.Id, cancellationToken);

        if (storedRole == null)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "not_exists",
                Description = "Role not exists"
            });
        }

        _roles[role.Id] = role;

        return IdentityResult.Success;
    }

    async public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellationToken)
    {
        var storedRole = await FindByIdAsync(role.Id, cancellationToken);
        if (storedRole is null) return default(T);

        var propertyInfo = role.GetType().GetProperty(applicationRoleProperty);
        if(propertyInfo is null) return default(T);

        propertyInfo.SetValue(storedRole, propertyValue, null);

        return propertyValue;
    }

    #endregion

    #region IAdminRoleDbContext

    public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
        => Task.FromResult(_roles.Values.Skip(skip).Take(limit));


    public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
        => Task.FromResult(_roles.Values.Where(r => r.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) == true));

    #endregion
}
