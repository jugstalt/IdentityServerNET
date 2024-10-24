using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.Distribution.Services;
using IdentityServer.Nova.Distribution.ValueTypes;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Models.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace IdentityServer.Nova.HttpProxy.Services.DbContext;

public class HttpProxyUserDb : IUserDbContext, IUserClaimsDbContext, IAdminUserDbContext, IUserRoleDbContext
{
    private readonly HttpInvokerService<IAdminUserDbContext> _httpInvoker;
    private readonly UserDbContextConfiguration _options;

    public HttpProxyUserDb(
            HttpInvokerService<IAdminUserDbContext> httpInvoker,
            IOptions<UserDbContextConfiguration>? options = null)
    {
        _httpInvoker = httpInvoker;
        _options = options?.Value ?? new UserDbContextConfiguration();
    }

    #region IUserDbContext

    public UserDbContextConfiguration ContextConfiguration => _options;

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationUser>(
                Helper.GetMethod<IUserDbContext>(nameof(CreateAsync)),
                user)!;

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationUser>(
                Helper.GetMethod<IUserDbContext>(nameof(DeleteAsync)),
                user)!;

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<ApplicationUser?>(
                Helper.GetMethod<IUserDbContext>(nameof(FindByEmailAsync)),
                normalizedEmail);

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<ApplicationUser?>(
                Helper.GetMethod<IUserDbContext>(nameof(FindByIdAsync)),
                userId);

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        => _httpInvoker.HandleGetAsync<ApplicationUser?>(
                Helper.GetMethod<IUserDbContext>(nameof(FindByNameAsync)),
                normalizedUserName);

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<IdentityResult, ApplicationUser>(
                Helper.GetMethod<IUserDbContext>(nameof(UpdateAsync)),
                user)!;

    public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
        => _httpInvoker.HandlePostAsync<T, ApplicationUser>(
                Helper.GetMethod<IUserDbContext>(nameof(UpdatePropertyAsync)),
                user, applicationUserProperty, propertyValue!)!;

    public Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserDbContext>(nameof(UpdatePropertyByEditorInfoAsync)),
                user, dbPropertyInfo, propertyValue);

    #endregion

    #region IUserClaimsDbContext

    public Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserClaimsDbContext>(nameof(AddClaimsAsync)),
                user, claims);

    public Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserClaimsDbContext>(nameof(RemoveClaimsAsync)),
                user, claims);

    public Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserClaimsDbContext>(nameof(ReplaceClaimAsync)),
                user, claim);

    #endregion

    #region IAdminUserDbContext

    async public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApplicationUser>>(
                Helper.GetMethod<IAdminUserDbContext>(nameof(GetUsersAsync)),
                limit, skip) ?? [];

    async public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IEnumerable<ApplicationUser>>(
                Helper.GetMethod<IAdminUserDbContext>(nameof(FindUsers)),
                term) ?? [];

    #endregion

    #region IUserRoleDbContext

    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserRoleDbContext>(nameof(AddToRoleAsync)),
                user, roleName);

    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        => _httpInvoker.HandlePostAsync<NoResult, ApplicationUser>(
                Helper.GetMethod<IUserRoleDbContext>(nameof(RemoveFromRoleAsync)),
                user, roleName);

    async public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        => await _httpInvoker.HandleGetAsync<IList<ApplicationUser>>(
                Helper.GetMethod<IUserRoleDbContext>(nameof(GetUsersInRoleAsync)),
                roleName) ?? [];

    #endregion
}
