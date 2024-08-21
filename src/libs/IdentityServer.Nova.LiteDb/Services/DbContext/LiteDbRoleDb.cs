using IdentityServer.Nova.Abstractions.Cryptography;
using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Serialize;
using IdentityServer.Nova.Abstractions.Services;
using IdentityServer.Nova.LiteDb.Documents;
using IdentityServer.Nova.LiteDb.Extensions;
using IdentityServer.Nova.Models;
using IdentityServer.Nova.Services.Serialize;
using LiteDB;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace IdentityServer.Nova.LiteDb.Services.DbContext;

public class LiteDbRoleDb : IRoleDbContext, IAdminRoleDbContext
{
    private readonly string _connectionString;
    private readonly RoleDbContextConfiguration _config;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string RolesCollectionName = "roles";

    public LiteDbRoleDb(
            IOptions<RoleDbContextConfiguration> options,
            ICryptoService cryptoService,
            IBlobSerializer? blobSerializer = null
        )
    {
        _config = options.Value;
        _connectionString = _config.ConnectionString.EnsureLiteDbParentDirectoryCreated();
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    #region IRoleDbContext

    async public Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        if (role == null)
        {
            return IdentityResult.Success;
        }

        role.Name = role.Name?.Trim().ToLowerInvariant();

        if (String.IsNullOrEmpty(role.Name))
        {
            throw new ArgumentException("Invalid username");
        }

        if (await FindByNameAsync(role.Name, cancellationToken) != null)
        {
            throw new Exception($"Role with name {role.Name} alread exists");
        }

        var blob = new LiteDbBlobDocument()
        {
            Name = role.Name,
            BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(role))
        };

        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(RolesCollectionName);

            var id = collection.Insert(blob);

            role.Id = id.RawValue.ToString();
            blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(role));

            collection.Update(blob);

            return IdentityResult.Success;
        }
    }

    public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        try
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(RolesCollectionName);

                collection.DeleteMany(b => b.Name == role.Name);

                return Task.FromResult(IdentityResult.Success);
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(
                new IdentityError() { Code = "999", Description = ex.Message }));
        }
    }

    public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(RolesCollectionName);

            ObjectId roleObjectId = new ObjectId(roleId);
            var blob = collection.FindById(roleObjectId);

            if (blob != null)
            {
                return Task.FromResult<ApplicationRole?>(
                        _blobSerializer.DeserializeObject<ApplicationRole>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ApplicationRole?>(null);
        }
    }

    public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(RolesCollectionName);
            var blob = collection.Query()
                                 .Where(x => x.Name == normalizedRoleName)
                                 .FirstOrDefault();

            if (blob != null)
            {
                return Task.FromResult<ApplicationRole?>(
                        _blobSerializer.DeserializeObject<ApplicationRole>(
                        _cryptoService.DecryptText(blob.BlobData))
                    );
            }

            return Task.FromResult<ApplicationRole?>(null);
        }
    }

    public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        try
        {
            using (var db = new LiteDatabase(_connectionString))
            {
                var collection = db.GetBlobDocumentCollection(RolesCollectionName);

                role.Name = role.Name?.Trim().ToLowerInvariant();

                var blob = collection.FindOne(b => b.Name == role.Name);
                if (blob == null)
                {
                    throw new Exception($"Resource with name = {role.Name} not exists");
                }

                blob.BlobData = _cryptoService.EncryptText(_blobSerializer.SerializeObject(role));

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

    async public Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellation)
    {
        var propertyInfo = role.GetType().GetProperty(applicationRoleProperty);
        if (propertyInfo != null)
        {
            propertyInfo.SetValue(role, propertyValue);

            await UpdateAsync(role, cancellation);
        }

        return propertyValue;
    }

    #endregion

    #region IAdminRoleDbContext

    public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(RolesCollectionName);

            var blobs = collection.FindAll();

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ApplicationRole>>(
                    blobs
                        .Skip(skip)
                        .Take(limit)
                        .Select(blob =>
                            _blobSerializer.DeserializeObject<ApplicationRole>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ApplicationRole>>(Array.Empty<ApplicationRole>());
        }
    }

    public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
    {
        using (var db = new LiteDatabase(_connectionString))
        {
            var collection = db.GetBlobDocumentCollection(RolesCollectionName);

            var blobs = collection.Find(u => u.Name.Contains(term));

            if (blobs != null)
            {
                return Task.FromResult<IEnumerable<ApplicationRole>>(
                    blobs
                        .Take(1000)
                        .Select(blob =>
                            _blobSerializer.DeserializeObject<ApplicationRole>(
                            _cryptoService.DecryptText(blob.BlobData))
                        ).ToArray()
                    );
            }

            return Task.FromResult<IEnumerable<ApplicationRole>>(Array.Empty<ApplicationRole>());
        }
    }

    #endregion
}
