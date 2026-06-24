using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.LiteDb.Documents;
using IdentityServerNET.LiteDb.Extensions;
using IdentityServerNET.Services.Serialize;
using LiteDB;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace IdentityServerNET.LiteDb.Services.DbContext;

public class LiteDbUIDb : IUIDbContext
{
    private readonly string _connectionString;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string CollectionName = "ui_settings";
    private const string SettingsName = "settings";

    public LiteDbUIDb(
        IOptions<UIDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _connectionString = options.Value.ConnectionString.EnsureLiteDbParentDirectoryCreated();
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();
    }

    public Task<UICustomizationSettings?> GetSettingsAsync()
    {
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetBlobDocumentCollection(CollectionName);
        var doc = col.FindOne(d => d.Name == SettingsName);
        if (doc is null) return Task.FromResult<UICustomizationSettings?>(null);
        return Task.FromResult(
            _blobSerializer.DeserializeObject<UICustomizationSettings?>(_cryptoService.DecryptText(doc.BlobData)));
    }

    public Task SaveSettingsAsync(UICustomizationSettings settings)
    {
        var encrypted = _cryptoService.EncryptText(_blobSerializer.SerializeObject(settings));
        using var db = new LiteDatabase(_connectionString);
        var col = db.GetBlobDocumentCollection(CollectionName);
        var existing = col.FindOne(d => d.Name == SettingsName);
        if (existing is null)
        {
            col.Insert(new LiteDbBlobDocument { Name = SettingsName, BlobData = encrypted });
        }
        else
        {
            existing.BlobData = encrypted;
            col.Update(existing);
        }
        return Task.CompletedTask;
    }
}
