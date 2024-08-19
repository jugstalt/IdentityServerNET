using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Serialize;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.LiteDb.Documents;
using IdentityServer.Nova.LiteDb.Extensions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Models.UserInteraction;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.Serialize;
using LiteDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace IdentityServer.Nova.LiteDb.Services.DbContext;

public class LiteDbUserDb : IUserDbContext, IAdminUserDbContext, IUserRoleDbContext
{
    private readonly string _connectionString;
    private readonly UserDbContextConfiguration _config;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string UsersCollectionName = "users";

    public LiteDbUserDb(IOptions<UserDbContextConfiguration> options)
    {
        _config = options.Value;
        _connectionString = _config.ConnectionString.EnsureLiteDbParentDirectoryCreated();
        _cryptoService = _config.CryptoService ?? new Base64CryptoService();
        _blobSerializer = _config.BlobSerializer ?? new JsonBlobSerializer();
    }

    #region IUserDbContext

    public UserDbContextConfiguration ContextConfiguration => _config;

    async public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user == null)
        {
            return IdentityResult.Success;
        }

        try
        {
            user.UserName = user.UserName?.Trim().ToLowerInvariant();
            user.Email = user.Email?.Trim().ToLowerInvariant();

            if (String.IsNullOrEmpty(user.UserName))
            {
                throw new ArgumentException("Invalid username");
            }

            if (string.IsNullOrEmpty(user.Email))
            {
                throw new ArgumentException("Invalid email");
            }

            if (await FindByNameAsync(user.UserName, cancellationToken) != null)
            {
                throw new ArgumentException($"User with name {user.UserName} alread exists");
            }

            if (await FindByEmailAsync(user.Email, cancellationToken) != null)
            {
                throw new ArgumentException($"User with name {user.UserName} alread exists");
            }

            var blob = new LiteDbBlobDocument()
            {
                Name = user.UserName,
                AltName = user.Email,
                //BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(user))
            };

            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(UsersCollectionName);

                var id = collection.Insert(blob);

                user.Id = id.RawValue.ToString();
                blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(user));

                collection.Update(blob);

                return IdentityResult.Success;
            }
        }
        catch (ArgumentException argEx)
        {
            return IdentityResult.Failed(
                new IdentityError() { Code = "999", Description = argEx.Message }
            );
        }
        catch
        {
            return IdentityResult.Failed(
                new IdentityError() { Code = "999", Description = "Unknown error" }
            );
        }
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(UsersCollectionName);

                collection.DeleteMany(b => b.Name == user.UserName);

                return Task.FromResult(IdentityResult.Success);
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError() { Code = "999", Description = ex.Message }));
        }
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(UsersCollectionName);

            var blob = collection.Query()
                                 .Where(x => x.AltName == normalizedEmail)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<ApplicationUser?>(
                        _blobSerializer.DeserializeObject<ApplicationUser>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ApplicationUser?>(null);
        }
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(UsersCollectionName);

                ObjectId userObjectId = new ObjectId(userId);
                var blob = collection.FindById(userObjectId);

                if (blob != null)
                {
                    return Task.FromResult<ApplicationUser?>(
                            _blobSerializer.DeserializeObject<ApplicationUser>(
                            _cryptoService.DecryptText(blob.BlobData))
                        );
                }
            }
        } catch {   }

        return Task.FromResult<ApplicationUser?>(null);
    }

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(UsersCollectionName);
            var blob = collection.Query()
                                 .Where(x => x.Name == normalizedUserName)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<ApplicationUser?>(
                        _blobSerializer.DeserializeObject<ApplicationUser>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ApplicationUser?>(null);
        }
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(UsersCollectionName);

                user.UserName = user.UserName?.Trim().ToLowerInvariant();
                user.Email = user.Email?.Trim().ToLowerInvariant();

                var blob = collection.FindOne(b => b.Name == user.UserName);
                if (blob == null)
                {
                    throw new Exception($"Resource with name = {user.UserName} not exists");
                }

                blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(user));

                collection.Update(blob);

                return Task.FromResult(IdentityResult.Success);
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(
                [
                    new IdentityError() { Code = "999", Description = ex.Message }
                ]));
        }
    }

    async public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
    {
        var propertyInfo = user.GetType().GetProperty(applicationUserProperty);
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(user, propertyValue);

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

    public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(UsersCollectionName);

            var blobs = collection.FindAll();

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ApplicationUser>>(
                    blobs
                        .Skip(skip)
                        .Take(limit)
                        .Select(blob =>
                            _blobSerializer.DeserializeObject<ApplicationUser>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ApplicationUser>>(Array.Empty<ApplicationUser>());
        }
    }

    public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(UsersCollectionName);

            var blobs = collection.Find(u => u.Name.Contains(term));

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ApplicationUser>>(
                    blobs
                        .Take(1000)
                        .Select(blob =>
                            _blobSerializer.DeserializeObject<ApplicationUser>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ApplicationUser>>(Array.Empty<ApplicationUser>());
        }
    }

    #endregion

    #region IUserRoleDbContext

    async public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var updateUser = await FindByIdAsync(user.Id, cancellationToken); // reload user

        if (updateUser is null)
        {
            throw new Exception("Can't update unknown user");
        }

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

        if (updateUser is null)
        {
            throw new Exception("Can't update unknown user");
        }

        if (updateUser.Roles != null && updateUser.Roles.Contains(roleName))
        {
            updateUser.Roles = updateUser.Roles.Where(r => r != roleName).ToArray();
            await UpdateAsync(updateUser, cancellationToken);
        }

        user.Roles = updateUser.Roles;
    }

    async public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = await GetUsersAsync(int.MaxValue, 0, cancellationToken);

        return users
                .Where(u => u.Roles != null && u.Roles.Contains(roleName))
                .ToList();
    }

    #endregion
}
