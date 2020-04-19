using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Stores
{
    public class RoleStoreProxy : IRoleStore<ApplicationRole>
    {
        private IRoleDbContext _roleDbContext = null;

        public RoleStoreProxy(IRoleDbContext roleDbContext = null)
        {
            _roleDbContext = roleDbContext ?? new InMemoryRoleDb();
        }

        public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return _roleDbContext.CreateAsync(role, cancellationToken);
        }

        public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return _roleDbContext.DeleteAsync(role, cancellationToken);
        }

        public void Dispose()
        {
            if(_roleDbContext is IDisposable)
            {
                ((IDisposable)_roleDbContext).Dispose();
            }
        }

        public Task<ApplicationRole> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        {
            return _roleDbContext.FindByIdAsync(roleId, cancellationToken);
        }

        public Task<ApplicationRole> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        {
            return _roleDbContext.FindByNameAsync(normalizedRoleName, cancellationToken);
        }

        public Task<string> GetNormalizedRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult<string>(role.Name.ToUpper());
        }

        public Task<string> GetRoleIdAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Id);
        }

        public Task<string> GetRoleNameAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return Task.FromResult(role.Name);
        }

       async  public Task SetNormalizedRoleNameAsync(ApplicationRole role, string normalizedName, CancellationToken cancellationToken)
        {
            role.NormalizedName =
                await _roleDbContext.UpdatePropertyAsync<string>(role, ApplicationRoleProperties.NormalizedName, normalizedName, cancellationToken);
        }

        async public Task SetRoleNameAsync(ApplicationRole role, string roleName, CancellationToken cancellationToken)
        {
            role.Name =
                await _roleDbContext.UpdatePropertyAsync<string>(role, ApplicationRoleProperties.Name, roleName, cancellationToken);
        }

        public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        {
            return _roleDbContext.UpdateAsync(role, cancellationToken);
        }
    }
}
