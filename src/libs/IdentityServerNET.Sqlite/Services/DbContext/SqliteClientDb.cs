using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.Serialize;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace IdentityServerNET.Sqlite.Services.DbContext;

public class SqliteClientDb : IClientDbContextModify
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "IsNetClients";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            Id TEXT NOT NULL PRIMARY KEY,
            Name TEXT NOT NULL,
            BlobData TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS IX_{TableName}_Name ON {TableName}(Name);
        """;

    public SqliteClientDb(
        IOptions<ClientDbContextConfiguration> options,
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

    private ClientModel? Deserialize(string blobData)
        => _blobSerializer.DeserializeObject<ClientModel>(_cryptoService.DecryptText(blobData));

    private string Serialize(ClientModel client)
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(client));

    #region IClientDbContext

    public Task<ClientModel?> FindClientByIdAsync(string clientId)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT Id, Name, BlobData FROM {TableName} WHERE Name = @Name",
            new { Name = clientId });
        return Task.FromResult(row != null ? Deserialize(row.BlobData) : null);
    }

    #endregion

    #region IClientDbContextModify

    public async Task AddClientAsync(ClientModel client)
    {
        if (client == null) return;

        if (await FindClientByIdAsync(client.ClientId) != null)
            throw new Exception("Client already exists");

        using var conn = OpenConnection();
        await conn.ExecuteAsync(
            $"INSERT INTO {TableName} (Id, Name, BlobData) VALUES (@Id, @Name, @BlobData)",
            new { Id = Guid.NewGuid().ToString(), Name = client.ClientId, BlobData = Serialize(client) });
    }

    public Task<IEnumerable<ClientModel>> GetAllClients()
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>($"SELECT Id, Name, BlobData FROM {TableName}");
        return Task.FromResult<IEnumerable<ClientModel>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    public Task RemoveClientAsync(ClientModel client)
    {
        using var conn = OpenConnection();
        conn.Execute($"DELETE FROM {TableName} WHERE Name = @Name", new { Name = client.ClientId });
        return Task.CompletedTask;
    }

    public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
    {
        using var conn = OpenConnection();
        int rows = conn.Execute(
            $"UPDATE {TableName} SET BlobData = @BlobData WHERE Name = @Name",
            new { BlobData = Serialize(client), Name = client.ClientId });

        if (rows == 0)
            throw new Exception($"Client with clientId '{client.ClientId}' not found");

        return Task.CompletedTask;
    }

    #endregion

    private record BlobDocument(string Id, string Name, string BlobData);
}
