﻿using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public class InMemoryUserDb : IUserDbContext, IUserClaimsDbContext, IAdminUserDbContext, IUserRoleDbContext
    {
        private static ConcurrentDictionary<string, ApplicationUser> _users = new ConcurrentDictionary<string, ApplicationUser>();

        private readonly UserDbContextConfiguration _config;

        public InMemoryUserDb(IOptionsMonitor<UserDbContextConfiguration> optionsMonitor = null)
        {
            _config = optionsMonitor?.CurrentValue ?? new UserDbContextConfiguration();
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
            var user = _users.Values
                .ToArray()
                .Where(u => u.Email.ToUpper() == normalizedEmail)
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
            var user = _users.Values
                .ToArray()
                .Where(u => u.UserName.ToUpper() == normalizedUserName)
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

        public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
        {
            return Task.FromResult<T>(propertyValue);
        }

        public Task UpdatePropertyAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation)
        {
            if (!String.IsNullOrWhiteSpace(dbPropertyInfo.ClaimName))
            {
                List<Claim> claims = new List<Claim>(user.Claims
                        .Where(c => c.Type != dbPropertyInfo.ClaimName));

                if (!String.IsNullOrWhiteSpace(propertyValue?.ToString()))
                {
                    claims.Add(new Claim(dbPropertyInfo.ClaimName, propertyValue?.ToString()));
                }

                user.Claims = claims;
            }

            return Task.CompletedTask;
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

        public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var roles = new List<string>();
            if (user.Roles != null)
            {
                roles.AddRange(user.Roles);
            }

            if (!roles.Contains(roleName))
            {
                roles.Add(roleName);
            }

            user.Roles = roles;

            return Task.CompletedTask;
        }

        public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            if (user.Roles != null)
            {
                var roles = new List<string>(user.Roles);
                if (roles.Contains(roleName))
                {
                    roles.Remove(roleName);
                    user.Roles = roles;
                }
            }

            return Task.CompletedTask;
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
}
