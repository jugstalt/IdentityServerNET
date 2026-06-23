using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Services.Serialize;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IdentityServerNET.SqlServer.Services.DbContext;

public class SqlServerRoleDb : IRoleDbContext, IAdminRoleDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetRoles";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        IF OBJECT_ID('{TableName}', 'U') IS NULL
        BEGIN
            CREATE TABLE {TableName} (
                Id NVARCHAR(450) NOT NULL CONSTRAINT PK_{TableName} PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL,
                BlobData NVARCHAR(MAX) NOT NULL
            );
            CREATE UNIQUE INDEX IX_{TableName}_Name ON {TableName}(Name);
        END
        """;

    public SqlServerRoleDb(
        IOptions<RoleDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _connectionString = options.Value.ConnectionString;
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

    private ApplicationRole? Deserialize(string blobData)
        => _blobSerializer.DeserializeObject<ApplicationRole>(_cryptoService.DecryptText(blobData));

    private string Serialize(ApplicationRole role)
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(role));

    #region IRoleDbContext

    public async Task<IdentityResult> CreateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        if (role == null) return IdentityResult.Success;

        role.Name = role.Name?.Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(role.Name))
            throw new ArgumentException("Invalid role name");

        if (await FindByNameAsync(role.Name, cancellationToken) != null)
            throw new Exception($"Role with name {role.Name} already exists");

        role.Id = Guid.NewGuid().ToString();

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            $"INSERT INTO {TableName} (Id, Name, BlobData) VALUES (@Id, @Name, @BlobData)",
            new { Id = role.Id, Name = role.Name, BlobData = Serialize(role) });

        return IdentityResult.Success;
    }

    public Task<IdentityResult> DeleteAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        try
        {
            using var conn = OpenConnection();
            conn.Execute($"DELETE FROM {TableName} WHERE Name = @Name", new { Name = role.Name });
            return Task.FromResult(IdentityResult.Success);
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "999", Description = ex.Message }));
        }
    }

    public Task<ApplicationRole?> FindByIdAsync(string roleId, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {TableName} WHERE Id = @Id",
            new { Id = roleId });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<ApplicationRole?> FindByNameAsync(string normalizedRoleName, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {TableName} WHERE Name = @Name",
            new { Name = normalizedRoleName.ToLowerInvariant() });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public Task<IdentityResult> UpdateAsync(ApplicationRole role, CancellationToken cancellationToken)
    {
        try
        {
            role.Name = role.Name?.Trim().ToLowerInvariant();

            using var conn = OpenConnection();
            int rows = conn.Execute(
                $"UPDATE {TableName} SET BlobData = @BlobData WHERE Name = @Name",
                new { BlobData = Serialize(role), Name = role.Name });

            if (rows == 0)
                throw new Exception($"Role with name '{role.Name}' not found");

            return Task.FromResult(IdentityResult.Success);
        }
        catch (Exception ex)
        {
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "999", Description = ex.Message }));
        }
    }

    public async Task<T> UpdatePropertyAsync<T>(ApplicationRole role, string applicationRoleProperty, T propertyValue, CancellationToken cancellation)
    {
        var prop = role.GetType().GetProperty(applicationRoleProperty);
        if (prop != null)
        {
            prop.SetValue(role, propertyValue);
            await UpdateAsync(role, cancellation);
        }
        return propertyValue;
    }

    #endregion

    #region IAdminRoleDbContext

    public Task<IEnumerable<ApplicationRole>> GetRolesAsync(int limit, int skip, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {TableName} ORDER BY Name OFFSET @Skip ROWS FETCH NEXT @Limit ROWS ONLY",
            new { Skip = skip, Limit = limit });
        return Task.FromResult<IEnumerable<ApplicationRole>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    public Task<IEnumerable<ApplicationRole>> FindRoles(string term, CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>(
            $"SELECT TOP 1000 Id, Name, BlobData FROM {TableName} WHERE Name LIKE @Term",
            new { Term = $"%{term}%" });
        return Task.FromResult<IEnumerable<ApplicationRole>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    #endregion

    private record BlobDocument(string Id, string Name, string BlobData);
}
