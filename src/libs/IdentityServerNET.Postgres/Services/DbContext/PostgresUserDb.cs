using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using IdentityServerNET.Services.Serialize;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace IdentityServerNET.Postgres.Services.DbContext;

public class PostgresUserDb : IUserDbContext, IAdminUserDbContext, IUserRoleDbContext
{
    private readonly string _connectionString;
    private readonly UserDbContextConfiguration _config;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "isnet_users";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            id VARCHAR(450) NOT NULL PRIMARY KEY,
            name VARCHAR(256) NOT NULL,
            alt_name VARCHAR(256) NULL,
            blob_data TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_{TableName}_name ON {TableName}(name);
        CREATE INDEX IF NOT EXISTS ix_{TableName}_alt_name ON {TableName}(alt_name);
        """;

    public PostgresUserDb(
        IOptions<UserDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _config = options.Value;
        _connectionString = _config.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    private NpgsqlConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        if (_initializedSchemas.TryAdd(_connectionString, true))
            conn.Execute(CreateTableSql);
        return conn;
    }

    private ApplicationUser? Deserialize(string blobData)
        => _blobSerializer.DeserializeObject<ApplicationUser>(_cryptoService.DecryptText(blobData));

    private string Serialize(ApplicationUser user)
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(user));

    #region IUserDbContext

    public string DefaultAdminLogin => Const.DefaultAdminLogin;
    public UserDbContextConfiguration ContextConfiguration => _config;

    public async Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        if (user == null) return IdentityResult.Success;

        try
        {
            user.UserName = user.UserName?.Trim().ToLowerInvariant();
            user.Email = user.Email?.Trim().ToLowerInvariant();

            if (string.IsNullOrEmpty(user.UserName))
                throw new ArgumentException("Invalid username");
            if (string.IsNullOrEmpty(user.Email))
                throw new ArgumentException("Invalid email");

            if (await FindByNameAsync(user.UserName, cancellationToken) != null)
                throw new ArgumentException($"User with name {user.UserName} already exists");
            if (await FindByEmailAsync(user.Email, cancellationToken) != null)
                throw new ArgumentException($"User with email {user.Email} already exists");

            user.Id = Guid.NewGuid().ToString();

            using var conn = OpenConnection();
            await conn.ExecuteAsync(
                $"INSERT INTO {TableName} (id, name, alt_name, blob_data) VALUES (@Id, @Name, @AltName, @BlobData)",
                new { Id = user.Id, Name = user.UserName, AltName = user.Email, BlobData = Serialize(user) });

            return IdentityResult.Success;
        }
        catch (ArgumentException ex)
        {
            return IdentityResult.Failed(new IdentityError { Code = "999", Description = ex.Message });
        }
        catch
        {
            return IdentityResult.Failed(new IdentityError { Code = "999", Description = "Unknown error" });
        }
    }

    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            using var conn = OpenConnection();
            conn.Execute($"DELETE FROM {TableName} WHERE name = @Name", new { Name = user.UserName });
            return Task.FromResult(IdentityResult.Success);
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "999", Description = ex.Message }));
        }
    }

    public Task<ApplicationUser?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT id, name, alt_name AS altname, blob_data AS blobdata FROM {TableName} WHERE alt_name = @AltName",
            new { AltName = normalizedEmail.ToLowerInvariant() });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT id, name, alt_name AS altname, blob_data AS blobdata FROM {TableName} WHERE id = @Id",
            new { Id = userId });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT id, name, alt_name AS altname, blob_data AS blobdata FROM {TableName} WHERE name = @Name",
            new { Name = normalizedUserName.ToLowerInvariant() });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        try
        {
            user.UserName = user.UserName?.Trim().ToLowerInvariant();
            user.Email = user.Email?.Trim().ToLowerInvariant();

            using var conn = OpenConnection();
            int rows = conn.Execute(
                $"UPDATE {TableName} SET alt_name = @AltName, blob_data = @BlobData WHERE name = @Name",
                new { AltName = user.Email, BlobData = Serialize(user), Name = user.UserName });

            if (rows == 0)
                throw new Exception($"User with name '{user.UserName}' not found");

            return Task.FromResult(IdentityResult.Success);
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "999", Description = ex.Message }));
        }
    }

    public async Task<T> UpdatePropertyAsync<T>(ApplicationUser user, string applicationUserProperty, T propertyValue, CancellationToken cancellation)
    {
        var prop = user.GetType().GetProperty(applicationUserProperty);
        if (prop != null)
        {
            prop.SetValue(user, propertyValue);
            await UpdateAsync(user, cancellation);
        }
        return propertyValue;
    }

    public async Task UpdatePropertyByEditorInfoAsync(ApplicationUser user, EditorInfo dbPropertyInfo, object propertyValue, CancellationToken cancellation)
    {
        var prop = user.GetType().GetProperty(dbPropertyInfo.Name);
        if (prop != null)
        {
            prop.SetValue(user, Convert.ChangeType(propertyValue, dbPropertyInfo.PropertyType!));
        }
        else if (!string.IsNullOrWhiteSpace(dbPropertyInfo.ClaimName))
        {
            var claims = new List<Claim>(user.Claims.Where(c => c.Type != dbPropertyInfo.ClaimName));
            if (!string.IsNullOrWhiteSpace(propertyValue?.ToString()))
                claims.Add(new Claim(dbPropertyInfo.ClaimName, propertyValue.ToString()!));
            user.Claims = claims;
        }
        await UpdateAsync(user, cancellation);
    }

    #endregion

    #region IAdminUserDbContext

    public Task<IEnumerable<ApplicationUser>> GetUsersAsync(int limit, int skip, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>(
            $"SELECT id, name, alt_name AS altname, blob_data AS blobdata FROM {TableName} ORDER BY name LIMIT @Limit OFFSET @Skip",
            new { Skip = skip, Limit = limit });
        return Task.FromResult<IEnumerable<ApplicationUser>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>(
            $"SELECT id, name, alt_name AS altname, blob_data AS blobdata FROM {TableName} WHERE name ILIKE @Term LIMIT 1000",
            new { Term = $"%{term}%" });
        return Task.FromResult<IEnumerable<ApplicationUser>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    #endregion

    #region IUserRoleDbContext

    public async Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var updateUser = await FindByIdAsync(user.Id, cancellationToken);
        if (updateUser is null) throw new Exception("Can't update unknown user");

        if (updateUser.Roles == null)
            updateUser.Roles = [roleName];
        else
        {
            var roles = new List<string>(updateUser.Roles);
            if (!roles.Contains(roleName))
            {
                roles.Add(roleName);
                updateUser.Roles = roles.ToArray();
            }
        }

        await UpdateAsync(updateUser, cancellationToken);
        user.Roles = updateUser.Roles;
    }

    public async Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken)
    {
        var updateUser = await FindByIdAsync(user.Id, cancellationToken);
        if (updateUser is null) throw new Exception("Can't update unknown user");

        if (updateUser.Roles != null && updateUser.Roles.Contains(roleName))
        {
            updateUser.Roles = updateUser.Roles.Where(r => r != roleName).ToArray();
            await UpdateAsync(updateUser, cancellationToken);
        }

        user.Roles = updateUser.Roles;
    }

    public async Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken)
    {
        var users = await GetUsersAsync(int.MaxValue, 0, cancellationToken);
        return users.Where(u => u.Roles != null && u.Roles.Contains(roleName)).ToList();
    }

    #endregion

    private record BlobDocument(string Id, string Name, string? AltName, string BlobData);
}
