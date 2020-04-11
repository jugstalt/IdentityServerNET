using IdentityModel;
using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.Serialize;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class FileBlobUserDb : IUserDbContext
    {
        private string _rootPath = null;
        private ICryptoService _cryptoService = null;
        private IBlobSerializer _blobSerializer;

        public FileBlobUserDb(IOptions<UserDbContextConfiguration> options)
        {
            if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
                throw new ArgumentException("FileBlobUserDb: no connection string defined");

            _rootPath = options.Value.ConnectionString;
            _cryptoService = options.Value.CryptoService ?? new Base64CryptoService();
            _blobSerializer = options.Value.BlobSerializer ?? new JsonBlobSerializer();

            DirectoryInfo di = new DirectoryInfo(_rootPath);
            if(!di.Exists)
            {
                di.Create();
            }
        }

        #region IUserDbContext

        async public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            if(user.UserName.ToUpper() != user.Email.ToUpper())
            {
                return IdentityResult.Failed(new IdentityError()
                {
                    Code = "invalid_username",
                    Description = "username and email must be identical"
                });
            }

            user.Id = UsernameToId(user);

            FileInfo fi = new FileInfo($"{ _rootPath }/{ user.Id }.user");

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
            FileInfo fi = new FileInfo($"{ _rootPath }/{ user.Id }.user");

            if(fi.Exists)
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
            FileInfo fi = new FileInfo($"{ _rootPath }/{ userId }.user");

            if(!fi.Exists)
            {
                return null;
            }

            using (var reader = File.OpenText(fi.FullName))
            {
                var fileText = await reader.ReadToEndAsync();

                fileText = _cryptoService.DecryptText(fileText);

                return _blobSerializer.DeserializeObject<ApplicationUser>(fileText);
            }
        }

        async public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
        {
            return await FindByIdAsync(normalizedUserName.NameToHexId(_cryptoService), cancellationToken);
        }

        async public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            FileInfo fi = new FileInfo($"{ _rootPath }/{ user.Id }.user");

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
            if(propertyInfo!=null)
            {
                propertyInfo.SetValue(user, propertyValue);
            }

            if(user.UserName.ToUpper()!=user.Email.ToUpper())
            {
                throw new Exception("username and email must be idential");
            }

            await UpdateAsync(user, cancellation);

            return propertyValue;
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
