using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IdentityServerNET.SqlServer.Services.DbContext;

public class SqlServerResourceDb : IResourceDbContextModify
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string ApiTableName = "IsNetApiResources";
    private const string IdentityTableName = "IsNetIdentityResources";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private static string CreateTableSql(string tableName) => $"""
        IF OBJECT_ID('{tableName}', 'U') IS NULL
        BEGIN
            CREATE TABLE {tableName} (
                Id NVARCHAR(450) NOT NULL CONSTRAINT PK_{tableName} PRIMARY KEY,
                Name NVARCHAR(256) NOT NULL,
                BlobData NVARCHAR(MAX) NOT NULL
            );
            CREATE UNIQUE INDEX IX_{tableName}_Name ON {tableName}(Name);
        END
        """;

    public SqlServerResourceDb(
        IOptions<ResourceDbContextConfiguration> options,
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
        {
            conn.Execute(CreateTableSql(ApiTableName));
            conn.Execute(CreateTableSql(IdentityTableName));
        }
        return conn;
    }

    private T? Deserialize<T>(string blobData)
        => _blobSerializer.DeserializeObject<T>(_cryptoService.DecryptText(blobData));

    private string Serialize<T>(T obj) where T : notnull
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(obj));

    #region IResourceDbContext

    public Task<ApiResourceModel?> FindApiResourceAsync(string name)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {ApiTableName} WHERE Name = @Name",
            new { Name = name });
        return Task.FromResult(row != null ? Deserialize<ApiResourceModel>(row.BlobData) : null);
    }

    public async Task<IEnumerable<ApiResourceModel>> FindApiResourcesByScopeAsync(IEnumerable<string> scopeNames)
    {
        var results = new List<ApiResourceModel>();
        foreach (var scope in scopeNames)
        {
            var resource = await FindApiResourceAsync(scope);
            if (resource != null)
                results.Add(resource);
        }
        return results;
    }

    public Task<IEnumerable<ApiResourceModel>> GetAllApiResources()
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>($"SELECT Id, Name, BlobData FROM {ApiTableName}");
        return Task.FromResult<IEnumerable<ApiResourceModel>>(
            rows.Select(r => Deserialize<ApiResourceModel>(r.BlobData)!).ToArray());
    }

    public Task<IdentityResourceModel?> FindIdentityResource(string name)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {IdentityTableName} WHERE Name = @Name",
            new { Name = name });
        return Task.FromResult(row != null ? Deserialize<IdentityResourceModel>(row.BlobData) : null);
    }

    public Task<IEnumerable<IdentityResourceModel>> GetAllIdentityResources()
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>($"SELECT Id, Name, BlobData FROM {IdentityTableName}");
        return Task.FromResult<IEnumerable<IdentityResourceModel>>(
            rows.Select(r => Deserialize<IdentityResourceModel>(r.BlobData)!).ToArray());
    }

    #endregion

    #region IResourceDbContextModify

    public async Task AddApiResourceAsync(ApiResourceModel apiResource)
    {
        if (apiResource == null) return;

        if (await FindApiResourceAsync(apiResource.Name) != null)
            throw new Exception("API resource already exists");

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            $"INSERT INTO {ApiTableName} (Id, Name, BlobData) VALUES (@Id, @Name, @BlobData)",
            new { Id = Guid.NewGuid().ToString(), Name = apiResource.Name, BlobData = Serialize(apiResource) });
    }

    public Task RemoveApiResourceAsync(ApiResourceModel apiResource)
    {
        using var conn = OpenConnection();
        conn.Execute($"DELETE FROM {ApiTableName} WHERE Name = @Name", new { Name = apiResource.Name });
        return Task.CompletedTask;
    }

    public Task UpdateApiResourceAsync(ApiResourceModel apiResource, IEnumerable<string>? propertyNames = null)
    {
        using var conn = OpenConnection();
        int rows = conn.Execute(
            $"UPDATE {ApiTableName} SET BlobData = @BlobData WHERE Name = @Name",
            new { BlobData = Serialize(apiResource), Name = apiResource.Name });

        if (rows == 0)
            throw new Exception($"API resource '{apiResource.Name}' not found");

        return Task.CompletedTask;
    }

    public async Task AddIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        if (identityResource == null) return;

        if (await FindIdentityResource(identityResource.Name) != null)
            throw new Exception("Identity resource already exists");

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            $"INSERT INTO {IdentityTableName} (Id, Name, BlobData) VALUES (@Id, @Name, @BlobData)",
            new { Id = Guid.NewGuid().ToString(), Name = identityResource.Name, BlobData = Serialize(identityResource) });
    }

    public Task RemoveIdentityResourceAsync(IdentityResourceModel identityResource)
    {
        using var conn = OpenConnection();
        conn.Execute($"DELETE FROM {IdentityTableName} WHERE Name = @Name", new { Name = identityResource.Name });
        return Task.CompletedTask;
    }

    public Task UpdateIdentityResourceAsync(IdentityResourceModel identityResource, IEnumerable<string>? propertyNames = null)
    {
        using var conn = OpenConnection();
        int rows = conn.Execute(
            $"UPDATE {IdentityTableName} SET BlobData = @BlobData WHERE Name = @Name",
            new { BlobData = Serialize(identityResource), Name = identityResource.Name });

        if (rows == 0)
            throw new Exception($"Identity resource '{identityResource.Name}' not found");

        return Task.CompletedTask;
    }

    #endregion

    private record BlobDocument(string Id, string Name, string BlobData);
}
