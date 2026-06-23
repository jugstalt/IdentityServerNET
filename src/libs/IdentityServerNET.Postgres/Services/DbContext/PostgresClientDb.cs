using Dapper;
using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using Npgsql;
using System.Collections.Concurrent;

namespace IdentityServerNET.Postgres.Services.DbContext;

public class PostgresClientDb : IClientDbContextModify
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string TableName = "isnet_clients";
    private static readonly ConcurrentDictionary<string, bool> _initializedSchemas = new();

    private const string CreateTableSql = $"""
        CREATE TABLE IF NOT EXISTS {TableName} (
            id VARCHAR(450) NOT NULL PRIMARY KEY,
            name VARCHAR(256) NOT NULL,
            blob_data TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS ix_{TableName}_name ON {TableName}(name);
        """;

    public PostgresClientDb(
        IOptions<ClientDbContextConfiguration> options,
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

    private ClientModel? Deserialize(string blobData)
        => _blobSerializer.DeserializeObject<ClientModel>(_cryptoService.DecryptText(blobData));

    private string Serialize(ClientModel client)
        => _cryptoService.EncryptText(_blobSerializer.SerializeObject(client));

    #region IClientDbContext

    public Task<ClientModel?> FindClientByIdAsync(string clientId)
    {
        using var conn = OpenConnection();
        var row = conn.QueryFirstOrDefault<BlobDocument>(
            $"SELECT id, name, blob_data AS blobdata FROM {TableName} WHERE name = @Name",
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
            $"INSERT INTO {TableName} (id, name, blob_data) VALUES (@Id, @Name, @BlobData)",
            new { Id = Guid.NewGuid().ToString(), Name = client.ClientId, BlobData = Serialize(client) });
    }

    public Task<IEnumerable<ClientModel>> GetAllClients()
    {
        using var conn = OpenConnection();
        var rows = conn.Query<BlobDocument>($"SELECT id, name, blob_data AS blobdata FROM {TableName}");
        return Task.FromResult<IEnumerable<ClientModel>>(
            rows.Select(r => Deserialize(r.BlobData)!).ToArray());
    }

    public Task RemoveClientAsync(ClientModel client)
    {
        using var conn = OpenConnection();
        conn.Execute($"DELETE FROM {TableName} WHERE name = @Name", new { Name = client.ClientId });
        return Task.CompletedTask;
    }

    public Task UpdateClientAsync(ClientModel client, IEnumerable<string>? propertyNames = null)
    {
        using var conn = OpenConnection();
        int rows = conn.Execute(
            $"UPDATE {TableName} SET blob_data = @BlobData WHERE name = @Name",
            new { BlobData = Serialize(client), Name = client.ClientId });

        if (rows == 0)
            throw new Exception($"Client with clientId '{client.ClientId}' not found");

        return Task.CompletedTask;
    }

    #endregion

    private record BlobDocument(string Id, string Name, string BlobData);
}
