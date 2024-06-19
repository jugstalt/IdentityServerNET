using IdentityServer.Nova.Extensions.DependencyInjection;
using IdentityServer.Nova.LiteDb.Documents;
using IdentityServer.Nova.LiteDb.Extensions;
using IdentityServer.Nova.Models.IdentityServerWrappers;
using IdentityServer.Nova.Services.Cryptography;
using IdentityServer.Nova.Services.DbContext;
using IdentityServer.Nova.Services.Serialize;
using IdentityServer.Nova.UserInteraction;
using IdentityServer4.Models;
using LiteDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

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
        if (String.IsNullOrEmpty(options?.Value?.ConnectionString))
        {
            throw new ArgumentException("LiteDbUserDb: no connection string defined");
        }

        _config = options.Value;
        _connectionString = _config.ConnectionString;
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

        user.UserName = user.UserName?.ToLowerInvariant();
        user.Email = user.Email?.ToLowerInvariant();

        if (String.IsNullOrEmpty(user.UserName)) throw new ArgumentException("Invalid username");
        if (string.IsNullOrEmpty(user.Email)) throw new ArgumentException("Invalid email");

        if (await FindByNameAsync(user.UserName, cancellationToken) != null)
        {
            throw new Exception($"User with name {user.UserName} alread exists");
        }

        if (await FindByEmailAsync(user.Email, cancellationToken)!= null)
        {
            throw new Exception($"User with name {user.UserName} alread exists");
        }

        var blob = new LiteDbBlobDocument()
        {
            Name = user.UserName,
            AltName = user.Email,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(user))
        };

        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(UsersCollectionName);

            collection.Insert(blob);

            return IdentityResult.Success;
        }
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
    {
        throw new NotImplementedException();
    }

    public Task UpdatePropertyAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation)
    {
        throw new NotImplementedException();
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

    #endregion

    #region IUserRoleDbContext

    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    #endregion
}
