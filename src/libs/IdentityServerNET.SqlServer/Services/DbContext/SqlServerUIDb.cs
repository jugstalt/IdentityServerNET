using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace IdentityServerNET.SqlServer.Services.DbContext;

public class SqlServerUIDb : IUIDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetUISettings";
    private const string SettingsId = "settings";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        IF OBJECT_ID('{TableName}', 'U') IS NULL
        BEGIN
            CREATE TABLE {TableName} (
                Id NVARCHAR(50) NOT NULL CONSTRAINT PK_{TableName} PRIMARY KEY,
                BlobData NVARCHAR(MAX) NOT NULL
            );
        END
        """;

    public SqlServerUIDb(
        IOptions<UIDbContextConfiguration> options,
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

    public Task<UICustomizationSettings?> GetSettingsAsync()
    {
        using var conn = OpenConnection();
        var blob = conn.QueryFirstOrDefault<string>(
            $"SELECT BlobData FROM {TableName} WHERE Id = @Id",
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
            IF EXISTS (SELECT 1 FROM {TableName} WHERE Id = @Id)
                UPDATE {TableName} SET BlobData = @BlobData WHERE Id = @Id
            ELSE
                INSERT INTO {TableName} (Id, BlobData) VALUES (@Id, @BlobData)
            """,
            new { Id = SettingsId, BlobData = blob });
        return Task.CompletedTask;
    }
}
