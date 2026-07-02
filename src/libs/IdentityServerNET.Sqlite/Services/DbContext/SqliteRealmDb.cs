using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using IdentityServerNET.Models.Extensions;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IdentityServerNET.Sqlite.Services.DbContext;

public class SqliteRealmDb : IRealmDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetRealms";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            BlobData TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName}_Name ON {TableName}(Name);
        """;

    public SqliteRealmDb(
        IOptions<RealmDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _connectionString = options.Value.ConnectionString;
        EnsureParentDirectory(_connectionString);
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    private static void EnsureParentDirectory(string connectionString)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
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
