using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Models;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace IdentityServer.Nova.HttpProxy.Services.DbContext;

public class HttpProxyRoleDb : IAdminRoleDbContext
{
    private readonly HttpInvokerService<IAdminRoleDbContext> _httpInvoker;

    public HttpProxyRoleDb(
            HttpInvokerService<IAdminRoleDbContext> httpInvoker)
    {
        _httpInvoker = httpInvoker;
    }

    #region IRoleDbContext

    public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationRole>(
                Helper.GetMethod<IRoleDbContext>(nameof(CreateAsync)),
                role)!;

    public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationRole>(
                Helper.GetMethod<IRoleDbContext>(nameof(DeleteAsync)),
                role)!;

    public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<ApplicationRole?>(
                Helper.GetMethod<IRoleDbContext>(nameof(FindByIdAsync)),
                roleId);

    public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<ApplicationRole?>(
                Helper.GetMethod<IRoleDbContext>(nameof(FindByNameAsync)),
                normalizedRoleName);

    public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationRole>(
                Helper.GetMethod<IRoleDbContext>(nameof(UpdateAsync)),
                role)!;

    public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellation)
        => _httpInvoker.HandlePostAsync<T, ApplicationRole>(
                Helper.GetMethod<IRoleDbContext>(nameof(UpdatePropertyAsync)),
                role, applicationRoleProperty, propertyValue!)!;

    #endregion

    #region IAdminRoleDbContext

    async public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApplicationRole>>(
                Helper.GetMethod<IAdminRoleDbContext>(nameof(FindRoles)),
                term) ?? [];

    async public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApplicationRole>>(
                Helper.GetMethod<IAdminRoleDbContext>(nameof(GetRolesAsync)),
                limit, skip) ?? [];


    #endregion
}
