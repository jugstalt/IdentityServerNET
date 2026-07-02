using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IdentityServerNET.SqlServer.Services.DbContext;

public class SqlServerRealmDb : IRealmDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetRealms";
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

    public SqlServerRealmDb(
        IOptions<RealmDbContextConfiguration> options,
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

    private RealmModel Deserialize(string blobData)
        => _blobSerializer.DeserializeObject<RealmModel>(_cryptoService.DecryptText(blobData));

    private string Serialize(RealmModel realm)
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(realm));

    public Task<RealmModel?> FindByNameAsync(string realmName, CancellationToken cancellationToken)
    {
        realmName = (realmName ?? "").Trim().ToLowerInvariant();
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {TableName} WHERE Name = @Name",
            new { Name = realmName });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    public async Task<RealmModel?> FindByDomainAsync(string domain, CancellationToken cancellationToken)
    {
        domain = (domain ?? "").Trim().ToLowerInvariant();
        var all = await GetAllAsync(cancellationToken);
        return all.FirstOrDefault(r => r.Domains.Any(d => string.Equals(d, domain, StringComparison.OrdinalIgnoreCase)));
    }

    public Task<IEnumerable<RealmModel>> GetAllAsync(CancellationToken cancellationToken)
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>($"SELECT Id, Name, BlobData FROM {TableName}");
        return Task.FromResult<IEnumerable<RealmModel>>(rows.Select(r => Deserialize(r.BlobData)).ToArray());
    }

    public async Task CreateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();
        if (await FindByNameAsync(realm.Name, cancellationToken) != null)
            throw new InvalidOperationException($"Realm '{realm.Name}' already exists.");
        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);
        realm.CreateDate = realm.CreateDate == default ? DateTime.UtcNow : realm.CreateDate;

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            $"INSERT INTO {TableName} (Id, Name, BlobData) VALUES (@Id, @Name, @BlobData)",
            new { Id = Guid.NewGuid().ToString(), Name = realm.Name, BlobData = Serialize(realm) });
    }

    public async Task UpdateAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        realm.NormalizeAndValidate();
        await this.EnsureDomainsAvailableAsync(realm, cancellationToken);

        using var conn = OpenConnection();
        int rows = conn.Execute(
            $"UPDATE {TableName} SET BlobData = @BlobData WHERE Name = @Name",
            new { BlobData = Serialize(realm), Name = realm.Name });
        if (rows == 0)
            throw new InvalidOperationException($"Realm '{realm.Name}' does not exist.");
    }

    public Task DeleteAsync(RealmModel realm, CancellationToken cancellationToken)
    {
        var name = (realm.Name ?? "").Trim().ToLowerInvariant();
        using var conn = OpenConnection();
        conn.Execute($"DELETE FROM {TableName} WHERE Name = @Name", new { Name = name });
        return Task.CompletedTask;
    }

    private record BlobDocument(string Id, string Name, string BlobData);
}
