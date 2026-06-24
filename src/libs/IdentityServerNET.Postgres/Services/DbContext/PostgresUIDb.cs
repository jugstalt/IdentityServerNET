using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace IdentityServerNET.Postgres.Services.DbContext;

public class PostgresUIDb : IUIDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "is_net_ui_settings";
    private const string SettingsId = "settings";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            id TEXT NOT NULL PRIMARY KEY,
            blob_data TEXT NOT NULL
        );
        """;

    public PostgresUIDb(
        IOptions<UIDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _connectionString = options.Value.ConnectionString;
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

    public Task<UICustomizationSettings?> GetSettingsAsync()
    {
        using var conn = OpenConnection();
        var blob = conn.QueryFirstOrDefault<string>(
            $"SELECT blob_data FROM {TableName} WHERE id = @Id",
            new { Id = SettingsId });

        if (blob is null) return Task.FromResult<UICustomizationSettings?>(null);
        return Task.FromResult(
            _blobSerializer.DeserializeObject<UICustomizationSettings>(_cryptoService.DecryptText(blob)));
    }

    public Task SaveSettingsAsync(UICustomizationSettings settings)
    {
        var blob = _cryptoService.EncryptText(_blobSerializer.SerializeObject(settings));
        using var conn = OpenConnection();
        conn.Execute($"""
            INSERT INTO {TableName} (id, blob_data) VALUES (@Id, @BlobData)
            ON CONFLICT (id) DO UPDATE SET blob_data = EXCLUDED.blob_data
            """,
            new { Id = SettingsId, BlobData = blob });
        return Task.CompletedTask;
    }
}
