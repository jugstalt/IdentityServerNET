using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.UserInteraction;
using IdentityServerNET.Services.Serialize;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace IdentityServerNET.SqlServer.Services.DbContext;

public class SqlServerUserDb : IUserDbContext, IAdminUserDbContext, IUserRoleDbContext
{
    private readonly string _connectionString;
    private readonly UserDbContextConfiguration _config;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetUsers";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        IF OBJECT_ID('{TableName}', 'U') IS NULL
        BEGIN
            CREATE TABLE {TableName} (
                Id NVARCHAR(450) NOT NULL CONSTRAINT PK_{TableName} PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL,
                AltName NVARCHAR(256) NULL,
                BlobData NVARCHAR(MAX) NOT NULL
            );
            CREATE UNIQUE INDEX IX_{TableName}_Name ON {TableName}(Name);
            CREATE INDEX IX_{TableName}_AltName ON {TableName}(AltName);
        END
        """;

    public SqlServerUserDb(
        IOptions<UserDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _config = options.Value;
        _connectionString = _config.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    private SqlConnection OpenConnection()
    {
        var conn = new SqlConnection(_connectionString);
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
                $"INSERT INTO {TableName} (Id, Name, AltName, BlobData) VALUES (@Id, @Name, @AltName, @BlobData)",
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
            conn.Execute($"DELETE FROM {TableName} WHERE Name = @Name", new { Name = user.UserName });
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
            $"SELECT Id, Name, AltName, BlobData FROM {TableName} WHERE AltName = @AltName",
            new { AltName = normalizedEmail.ToLowerInvariant() });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, AltName, BlobData FROM {TableName} WHERE Id = @Id",
            new { Id = userId });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, AltName, BlobData FROM {TableName} WHERE Name = @Name",
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
                $"UPDATE {TableName} SET AltName = @AltName, BlobData = @BlobData WHERE Name = @Name",
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
            $"SELECT Id, Name, AltName, BlobData FROM {TableName} ORDER BY Name OFFSET @Skip ROWS FETCH NEXT @Limit ROWS ONLY",
            new { Skip = skip, Limit = limit });
        return Task.FromResult<IEnumerable<ApplicationUser>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    public Task<IEnumerable<ApplicationUser>> FindUsers(string term, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>(
            $"SELECT TOP 1000 Id, Name, AltName, BlobData FROM {TableName} WHERE Name LIKE @Term",
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
