using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace IdentityServerNET.Sqlite.Services.DbContext;

public class SqliteUIDb : IUIDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetUISettings";
    private const string SettingsId = "settings";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            Id TEXT NOT NULL PRIMARY KEY,
            BlobData TEXT NOT NULL
        );
        """;

    public SqliteUIDb(
        IOptions<UIDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _connectionString = options.Value.ConnectionString;

        var builder = new SqliteConnectionStringBuilder(_connectionString);
        if (!string.IsNullOrEmpty(builder.DataSource) && builder.DataSource != ":memory:")
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(builder.DataSource));
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }

        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    private SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        if (_initializedSchemas.TryAdd(_connectionString, true))
            conn.Execute(CreateTableSql);
        return conn;
    }

    public Task<UICustomizationSettings?> GetSettingsAsync()
    {
        using var conn = OpenConnection();
        var blob = conn.QueryFirstOrDefault<string>(
            $"SELECT BlobData FROM {TableName} WHERE Id = @Id",
            new { Id = SettingsId });

        if (blob is null) return Task.FromResult<UICustomizationSettings?>(null);
        return Task.FromResult(
            _blobSerializer.DeserializeObject<UICustomizationSettings?>(_cryptoService.DecryptText(blob)));
    }

    public Task SaveSettingsAsync(UICustomizationSettings settings)
    {
        var blob = _cryptoService.EncryptText(_blobSerializer.SerializeObject(settings));
        using var conn = OpenConnection();
        conn.Execute($"""
            INSERT INTO {TableName} (Id, BlobData) VALUES (@Id, @BlobData)
            ON CONFLICT(Id) DO UPDATE SET BlobData = excluded.BlobData
            """,
            new { Id = SettingsId, BlobData = blob });
        return Task.CompletedTask;
    }
}
