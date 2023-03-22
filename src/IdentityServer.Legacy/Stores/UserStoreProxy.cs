using IdentityServer.Legacy.Services.DbContext;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Stores
{
    public class UserStoreProxy : IUserStore<ApplicationUser>,
                                  IUserEmailStore<ApplicationUser>,
                                  IUserPasswordStore<ApplicationUser>,
                                  IUserPhoneNumberStore<ApplicationUser>,
                                  IUserAuthenticatorKeyStore<ApplicationUser>,
                                  IUserTwoFactorStore<ApplicationUser>,
                                  IUserTwoFactorRecoveryCodeStore<ApplicationUser>,
                                  IUserClaimStore<ApplicationUser>,
                                  IUserClaimsPrincipalFactory<ApplicationUser>,
                                  IUserLoginStore<ApplicationUser>,
                                  IUserSecurityStampStore<ApplicationUser>,
                                  IUserRoleStore<ApplicationUser>,
                                  IUserLockoutStore<ApplicationUser>

    {
        //private IUserDbContext _dbContext;
        //private IPasswordHasher<ApplicationUser> _passwordHasher = null;
        private readonly IUserStoreFactory _userStoreFactory;

        public UserStoreProxy(IUserStoreFactory userStoreFactory)
        {
            _userStoreFactory = userStoreFactory;

            //_dbContext = dbContextes.FirstOrDefault() ?? new InMemoryUserDb();
            //_passwordHasher = passwordHashers.FirstOrDefault();

            // ToDo: ILogging instead of Console.WriteLine
            //Console.WriteLine("Initialize UserStoreProxy");
            //Console.WriteLine("DbContext:      " + _dbContext.GetType().ToString());
            //Console.WriteLine("PasswordHasher: " + _passwordHasher.GetType().ToString());

            // Create Test Users

            //if (_appendTestUsers)
            //{
            //    _appendTestUsers = false;

            //    CreateAsync(
            //        new ApplicationUser()
            //        {
            //            UserName = "test1",
            //            PasswordHash = "test1",
            //            EmailConfirmed = true,
            //            Email = "test1@xyz.com"
            //        }, CancellationToken.None).Wait();

            //    if (passwordHasher != null) 
            //    {
            //        foreach (var userId in _users.Keys)
            //        {
            //            var user = _users[userId];
            //            user.PasswordHash = passwordHasher.HashPassword(user, user.PasswordHash);
            //        }
            //    }
            //}
        }

        #region IUserStore

        async public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).CreateAsync(user, cancellationToken);
        }

        async public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).DeleteAsync(user, cancellationToken);
        }

        public void Dispose()
        {

        }


        async public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).FindByIdAsync(userId, cancellationToken);
        }

        async public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).FindByNameAsync(normalizedUserName, cancellationToken);
        }

        public Task<string> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (String.IsNullOrWhiteSpace(user?.UserName))
            {
                throw new Exception("Invalid username");
            }

            return Task.FromResult(user.UserName?.ToUpper());
        }

        public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.UserName);
        }

        async public Task SetNormalizedUserNameAsync(ApplicationUser user, string normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName =
                await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.NormalizedUserName, normalizedName, cancellationToken);
        }

        async public Task SetUserNameAsync(ApplicationUser user, string userName, CancellationToken cancellationToken)
        {
            user.UserName =
                await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.UserName, userName, cancellationToken);
        }

        async public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).UpdateAsync(user, cancellationToken);
        }

        #endregion

        #region IUserEmailStore 

        async public Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return await (await UserDbContext()).FindByEmailAsync(normalizedEmail, cancellationToken);
        }

        public Task<string> GetEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email);
        }

        public Task<bool> GetEmailConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<string> GetNormalizedEmailAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.Email?.ToUpper());
        }

        async public Task SetEmailAsync(ApplicationUser user, string email, CancellationToken cancellationToken)
        {
            user.Email =
                await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.Email, email, cancellationToken);
        }

        async public Task SetEmailConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.EmailConfirmed =
                await (await UserDbContext()).UpdatePropertyAsync<bool>(user, ApplicationUserProperties.EmailConfirmed, confirmed, cancellationToken);
        }

        async public Task SetNormalizedEmailAsync(ApplicationUser user, string normalizedEmail, CancellationToken cancellationToken)
        {
            user.NormalizedEmail =
                await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.NormalizedEmail, normalizedEmail, cancellationToken);
        }

        #endregion

        #region IUserPasswordStore

        async public Task SetPasswordHashAsync(ApplicationUser user, string passwordHash, CancellationToken cancellationToken)
        {
            user.PasswordHash =
                await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.PasswordHash, passwordHash, cancellationToken);
        }

        public Task<string> GetPasswordHashAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(!String.IsNullOrEmpty(user.PasswordHash));
        }

        #endregion

        #region IUserPhoneNumberStore

        async public Task SetPhoneNumberAsync(ApplicationUser user, string phoneNumber, CancellationToken cancellationToken)
        {
            user.PhoneNumber =
               await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.PhoneNumber, phoneNumber, cancellationToken);
        }

        public Task<string> GetPhoneNumberAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user?.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        async public Task SetPhoneNumberConfirmedAsync(ApplicationUser user, bool confirmed, CancellationToken cancellationToken)
        {
            user.PhoneNumberConfirmed =
               await (await UserDbContext()).UpdatePropertyAsync<bool>(user, ApplicationUserProperties.PhoneNumberConfirmed, confirmed, cancellationToken);
        }

        #endregion

        #region IUserAuthenticatorKeyStore

        async public Task SetAuthenticatorKeyAsync(ApplicationUser user, string key, CancellationToken cancellationToken)
        {
            user.AuthenticatorKey =
               await (await UserDbContext()).UpdatePropertyAsync<string>(user, ApplicationUserProperties.AuthenticatorKey, key, cancellationToken);
        }

        public Task<string> GetAuthenticatorKeyAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AuthenticatorKey);
        }

        #endregion

        #region IUserTwoFactorStore

        async public Task SetTwoFactorEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            user.TwoFactorEnabled =
               await (await UserDbContext()).UpdatePropertyAsync<bool>(user, ApplicationUserProperties.TwoFactorEnabled, enabled, cancellationToken);
        }

        public Task<bool> GetTwoFactorEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.TwoFactorEnabled);
        }

        #endregion

        #region IUserTwoFactorRecoveryCodeStore

        async public Task ReplaceCodesAsync(ApplicationUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken)
        {
            user.TfaRecoveryCodes = recoveryCodes;

            user.TfaRecoveryCodes =
               await (await UserDbContext()).UpdatePropertyAsync<IEnumerable<string>>
                    (user, ApplicationUserProperties.TfaRecoveryCodes, recoveryCodes, cancellationToken);
        }

        async public Task<bool> RedeemCodeAsync(ApplicationUser user, string code, CancellationToken cancellationToken)
        {
            if (user.TfaRecoveryCodes == null || !user.TfaRecoveryCodes.Contains(code))
            {
                return false;
            }

            user.TfaRecoveryCodes = await (await UserDbContext()).UpdatePropertyAsync<IEnumerable<string>>
                    (user, ApplicationUserProperties.TfaRecoveryCodes,
                    user.TfaRecoveryCodes.Where(c => c != code).ToArray(),
                    cancellationToken);

            return true;
        }

        public Task<int> CountCodesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user.TfaRecoveryCodes == null)
            {
                return Task.FromResult(0);
            }

            return Task.FromResult(user.TfaRecoveryCodes.Count());
        }

        #endregion

        #region IUserClaimStore

        public Task<IList<Claim>> GetClaimsAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<Claim>>(user.Claims.ToList());
        }

        async public Task AddClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();
            if (dbContext is IUserClaimsDbContext)
            {
                await ((IUserClaimsDbContext)dbContext).AddClaimsAsync(user, claims, cancellationToken);
            }

            user.Claims = claims.ToArray();
        }

        async public Task ReplaceClaimAsync(ApplicationUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();
            if (dbContext is IUserClaimsDbContext)
            {
                await ((IUserClaimsDbContext)dbContext).ReplaceClaimAsync(user, claim, newClaim, cancellationToken);
            }
        }

        async public Task RemoveClaimsAsync(ApplicationUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();
            if (dbContext is IUserClaimsDbContext)
            {
                await ((IUserClaimsDbContext)dbContext).RemoveClaimsAsync(user, claims, cancellationToken);
            }
        }

        public Task<IList<ApplicationUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<ApplicationUser>>(null);
        }

        #endregion

        #region IUserClaimsPrincipalFactory

        public Task<ClaimsPrincipal> CreateAsync(ApplicationUser user)
        {
            return Task.FromResult(new ClaimsPrincipal());
        }

        #endregion

        #region IUserLoginStore  (ToDo)

        public Task AddLoginAsync(ApplicationUser user, UserLoginInfo login, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task RemoveLoginAsync(ApplicationUser user, string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ApplicationUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IUserSecurityStampStore

        async public Task SetSecurityStampAsync(ApplicationUser user, string stamp, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            user.SecurityStamp =
                await dbContext.UpdatePropertyAsync<string>(user, ApplicationUserProperties.SecurityStamp, stamp, cancellationToken);
        }

        public Task<string> GetSecurityStampAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.SecurityStamp);
        }

        #endregion

        #region IUserRoleStore

        async public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            if (dbContext is IUserRoleDbContext)
            {
                await ((IUserRoleDbContext)dbContext).AddToRoleAsync(user, roleName, cancellationToken);
            }
        }

        async public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            if (dbContext is IUserRoleDbContext)
            {
                await ((IUserRoleDbContext)dbContext).RemoveFromRoleAsync(user, roleName, cancellationToken);
            }
        }

        public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult<IList<string>>(user.Roles?.ToArray() ?? new string[0]);
        }

        public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var isInRole = user.Roles != null && user.Roles.Contains(roleName);

            return Task.FromResult(isInRole);
        }

        async public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            if (dbContext is IUserRoleDbContext)
            {
                return await ((IUserRoleDbContext)dbContext).GetUsersInRoleAsync(roleName, cancellationToken);
            }

            return new ApplicationUser[0];
        }

        #endregion

        #region IUserLockoutStore

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnd);
        }

        async public Task SetLockoutEndDateAsync(ApplicationUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            user.LockoutEnd =
                await dbContext.UpdatePropertyAsync<DateTimeOffset?>(user, ApplicationUserProperties.LockoutEnd, lockoutEnd, cancellationToken);
        }

        async public Task<int> IncrementAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            user.AccessFailedCount =
                await dbContext.UpdatePropertyAsync<int>(user, ApplicationUserProperties.AccessFailedCount, user.AccessFailedCount + 1, cancellationToken);

            return user.AccessFailedCount;
        }

        async public Task ResetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            user.AccessFailedCount =
                await dbContext.UpdatePropertyAsync<int>(user, ApplicationUserProperties.AccessFailedCount, 0, cancellationToken);
        }

        public Task<int> GetAccessFailedCountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            return Task.FromResult(user.LockoutEnabled);
        }

        async public Task SetLockoutEnabledAsync(ApplicationUser user, bool enabled, CancellationToken cancellationToken)
        {
            var dbContext = await UserDbContext();

            user.LockoutEnabled =
                await dbContext.UpdatePropertyAsync<bool>(user, ApplicationUserProperties.LockoutEnabled, enabled, cancellationToken);
        }

        #endregion

        #region Helper

        private Task<IUserDbContext> UserDbContext()
        {
            return _userStoreFactory.CreateUserDbContextInstance();
        }

        #endregion
    }
}
