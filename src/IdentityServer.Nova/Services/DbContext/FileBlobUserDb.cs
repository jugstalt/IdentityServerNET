using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;
using IdentityServer.Nova.UserInteraction;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext
{
    public class FileBlobUserDb : IUserDbContext, IAdminUserDbContext, IUserRoleDbContext
    {
        private string _rootPath = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer;
        private UserDbContextConfiguration _config;

        public FileBlobUserDb(IOptionsMonitor<UserDbContextConfiguration> options = null)
        {
            if (String.IsNullOrEmpty(options?.CurrentValue?.ConnectionString))
            {
                throw new ArgumentException("FileBlobUserDb: no connection string defined");
            }

            _config = options.CurrentValue;
            _rootPath = _config.ConnectionString;
            _cryptoService = _config.CryptoService ?? new Base64CryptoService();
            _blobSerializer = _config.BlobSerializer ?? new JsonBlobSerializer();

            DirectoryInfo di = new DirectoryInfo(_rootPath);
            if (!di.Exists)
            {
                di.Create();
            }
        }

        #region IUserDbContext

        public UserDbContextConfiguration ContextConfiguration => _config;

        async public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if (user.UserName.ToUpper() != user.Email.ToUpper())
            {
                return IdentityResult.Failed(new IdentityError()
                {
                    Code = "invalid_username",
                    Description = "username and email must be identical"
                });
            }

            user.Id = UsernameToId(user);

            FileInfo fi = new FileInfo($"{_rootPath}/{user.Id}.user");

            if (fi.Exists)
            {
                return IdentityResult.Failed(new IdentityError()
                {
                    Code = "already_exists",
                    Description = "User already exists"
                });
            }

            byte[] buffer = Encoding.UTF8.GetBytes(
                _cryptoService.EncryptText(_blobSerializer.SerializeObject(user)));

            using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                            FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }

            return IdentityResult.Success;
        }

        public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            FileInfo fi = new FileInfo($"{_rootPath}/{user.Id}.user");

            if (fi.Exists)
            {
                fi.Delete();
            }

            return Task.FromResult(IdentityResult.Success);
        }

        async public Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
        {
            return await FindByNameAsync(normalizedEmail, cancellationToken);
        }

        async public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
        {
            FileInfo fi = new FileInfo($"{_rootPath}/{userId}.user");

            if (!fi.Exists)
            {
                return null;
            }

            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();

                fileText = _cryptoService.DecryptText(fileText);

                var applicationUser = _blobSerializer.DeserializeObject<ApplicationUser>(fileText);

                return applicationUser;
            }
        }

        public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return FindByIdAsync(normalizedUserName.NameToHexId(_cryptoService), cancellationToken);
        }

        async public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            FileInfo fi = new FileInfo($"{_rootPath}/{user.Id}.user");

            if (!fi.Exists)
            {
                return IdentityResult.Failed(new IdentityError()
                {
                    Code = "not_exists",
                    Description = "User not exists"
                });
            }
            fi.Delete();

            byte[] buffer = Encoding.UTF8.GetBytes(
                _cryptoService.EncryptText(_blobSerializer.SerializeObject(user)));

            using (var fs = new FileStream(fi.FullName, FileMode.OpenOrCreate,
                            FileAccess.Write, FileShare.None, buffer.Length, true))
            {
                await fs.WriteAsync(buffer, 0, buffer.Length);
            }

            return IdentityResult.Success;
        }

        async public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
        {
            var propertyInfo = user.GetType().GetProperty(applicationUserProperty);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(user, propertyValue);


                if (user.UserName.ToUpper() != user.Email.ToUpper())
                {
                    throw new Exception("username and email must be idential");
                }



                await UpdateAsync(user, cancellation);
            }

            return propertyValue;
        }

        async public Task UpdatePropertyAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation)
        {
            var propertyInfo = user.GetType().GetProperty(dbPropertyInfo.Name);
            if (propertyInfo != null)
            {
                propertyInfo.SetValue(user, Convert.ChangeType(propertyValue, dbPropertyInfo.PropertyType));

                if (user.UserName.ToUpper() != user.Email.ToUpper())
                {
                    throw new Exception("username and email must be idential");
                }

                await UpdateAsync(user, cancellation);
            }
            else
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

                await UpdateAsync(user, cancellation);
            }
        }

        #endregion

        #region IAdminUserDbContext

        async public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken)
        {
            List<ApplicationUser> users = new List<ApplicationUser>();
            foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.user").Skip(skip))
            {
                if (limit > 0 && users.Count >= limit)
                {
                    break;
                }

                using (var reader = File.OpenText(fi.FullName))
                {
                    var fileText = await reader.ReadToEndAsync();

                    fileText = _cryptoService.DecryptText(fileText);

                    users.Add(_blobSerializer.DeserializeObject<ApplicationUser>(fileText));
                }
            }

            return users.OrderBy(u => u.UserName);
        }

        #endregion

        #region IUserRoleDbContext

        async public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var updateUser = await FindByIdAsync(user.Id, cancellationToken); // reload user

            if (updateUser.Roles == null)
            {
                updateUser.Roles = new string[] { roleName };

                await UpdateAsync(updateUser, cancellationToken);
            }
            else
            {
                List<string> roles = new List<string>(updateUser.Roles);
                if (!roles.Contains(roleName))
                {
                    roles.Add(roleName);
                    updateUser.Roles = roles.ToArray();
                    await UpdateAsync(updateUser, cancellationToken);
                }
            }

            user.Roles = updateUser.Roles;
        }

        async public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
        {
            var updateUser = await FindByIdAsync(user.Id, cancellationToken); // reload user

            if (updateUser.Roles != null && updateUser.Roles.Contains(roleName))
            {
                updateUser.Roles = updateUser.Roles.Where(r => r != roleName).ToArray();
                await UpdateAsync(updateUser, cancellationToken);
            }

            user.Roles = updateUser.Roles;
        }

        async public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
        {
            var users = await GetUsersAsync(0, 0, cancellationToken);

            return users
                    .Where(u => u.Roles != null && u.Roles.Contains(roleName))
                    .ToList();
        }

        #endregion

        #region Helper

        private string UsernameToId(ApplicationUser user)
        {
            return user.UserName.NameToHexId(_cryptoService);
        }

        #endregion
    }
}
