using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Models.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext;

public class InMemoryUserDb : IUserDbContext, IUserClaimsDbContext, IAdminUserDbContext, IUserRoleDbContext
{
    private static ConcurrentDictionary<string, ApplicationUser> _users = new ConcurrentDictionary<string, ApplicationUser>();

    private readonly UserDbContextConfiguration _config;

    public InMemoryUserDb(IOptions<UserDbContextConfiguration> options = null)
    {
        _config = options?.Value ?? new UserDbContextConfiguration();
    }

    #region IUserDbContext

    public UserDbContextConfiguration ContextConfiguration => _config;

    async public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (await FindByIdAsync(user.Id, cancellationToken) != null &&
            await FindByNameAsync(user.UserName, cancellationToken) != null)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "already_exists",
                Description = "User already exists"
            });
        }

        if (String.IsNullOrWhiteSpace(user.Id))
        {
            user.Id = Guid.NewGuid().ToString().ToLower();
        }

        _users.TryAdd(user.Id, user);

        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (!_users.ContainsKey(user.Id))
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError()
            {
                Code = "not_exists",
                Description = "User not exists"
            }));
        }

        _users.TryRemove(user.Id, out user);

        return Task.FromResult(IdentityResult.Success);
    }

    public Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(normalizedEmail))
        {
            return null;
        }

        var user = _users.Values
            .ToArray()
            .Where(u => normalizedEmail.Equals(u.Email, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        return Task.FromResult(user);
    }

    public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        if (!_users.ContainsKey(userId))
        {
            return Task.FromResult<ApplicationUser>(null);
        }

        return Task.FromResult(_users[userId]);
    }

    public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        if (String.IsNullOrWhiteSpace(normalizedUserName))
        {
            return null;
        }

        var user = _users.Values
            .ToArray()
            .Where(u => normalizedUserName.Equals(u.UserName, StringComparison.OrdinalIgnoreCase))
            .FirstOrDefault();

        return Task.FromResult(user);
    }

    async public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var storedUser = await FindByIdAsync(user.Id, cancellationToken);

        if (storedUser == null)
        {
            return IdentityResult.Failed(new IdentityError()
            {
                Code = "not_exists",
                Description = "User not exists"
            });
        }

        _users[user.Id] = user;

        return IdentityResult.Success;
    }

    async public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellationToken)
    {
        var storedUser = await FindByIdAsync(user.Id, cancellationToken);

        if (storedUser is null) throw new ArgumentException("Unknown user");

        var propertyInfo = storedUser.GetType().GetProperty(applicationUserProperty);
        if (propertyInfo is null) throw new ArgumentException($"Unknown user property: {applicationUserProperty}");

        propertyInfo.SetValue(storedUser, propertyValue, null);

        return propertyValue;
    }

    async public Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellationToken)
    {
        if (!String.IsNullOrWhiteSpace(dbPropertyInfo.ClaimName))
        {
            var storedUser = await FindByIdAsync(user.Id, cancellationToken);
            if (storedUser is null) throw new ArgumentException("Unknown user");

            var propertyInfo = storedUser.GetType().GetProperty(
                dbPropertyInfo.ClaimName, 
                BindingFlags.IgnoreCase | BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
            if(propertyInfo is not null)
            {
                propertyInfo.SetValue(storedUser, propertyValue);

                return;
            }

            List<Claim> claims = new List<Claim>(storedUser.Claims
                    .Where(c => c.Type != dbPropertyInfo.ClaimName));

            if (!String.IsNullOrWhiteSpace(propertyValue?.ToString()))
            {
                claims.Add(new Claim(dbPropertyInfo.ClaimName, propertyValue?.ToString()));
            }

            storedUser.Claims = claims;
        }
    }

    #endregion

    #region IUserClaimsDbContext

    public Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        user.Claims = claims.ToArray();

        return Task.CompletedTask;
    }

    public Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    #endregion

    #region IAdminUserDbContext

    public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken)
        => Task.FromResult(
                _users.Values.Skip(skip).Take(limit)
            );

    public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken)
        => Task.FromResult(
                _users.Values.Where(u => u.UserName.Contains(term, StringComparison.OrdinalIgnoreCase))
            );

    #endregion

    #region IUserRoleContext

    async public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var storedUser = await FindByIdAsync(user.Id, cancellationToken);
        if (storedUser is null) throw new ArgumentException("Unknown user");

        var roles = new List<string>();
        if (storedUser.Roles != null)
        {
            roles.AddRange(user.Roles);
        }

        if (!roles.Contains(roleName))
        {
            roles.Add(roleName);
        }

        storedUser.Roles = roles;
    }

    async public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var storedUser = await FindByIdAsync(user.Id, cancellationToken);
        if (storedUser is null) throw new ArgumentException("Unknown user");

        if (storedUser.Roles != null)
        {
            var roles = new List<string>(storedUser.Roles);
            if (roles.Contains(roleName))
            {
                roles.Remove(roleName);
                storedUser.Roles = roles;
            }
        }
    }

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        return Task.FromResult<IList<ApplicationUser>>(
                _users.Values
                      .Where(u => u.Roles != null && u.Roles.Contains(roleName))
                      .ToList()
            );
    }

    #endregion
}
