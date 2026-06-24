#nullable enable

using IdentityServerNET.Abstractions.Cryptography;
using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Serialize;
using IdentityServerNET.Abstractions.UI;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using IdentityServerNET.Abstractions.Services;

namespace IdentityServerNET.Services.DbContext;

public class FileBlobUIDb : IUIDbContext
{
    private readonly string _rootPath;
    private readonly ICryptoService _cryptoService;
    private readonly IBlobSerializer _blobSerializer;

    private const string SettingsFileName = "ui-settings.json";

    public FileBlobUIDb(
        IOptions<UIDbContextConfiguration> options,
        ICryptoService cryptoService,
        IBlobSerializer? blobSerializer = null)
    {
        _rootPath = options.Value.ConnectionString;
        _cryptoService = cryptoService;
        _blobSerializer = blobSerializer ?? new JsonBlobSerializer();

        Directory.CreateDirectory(_rootPath);
    }

    public Task<UICustomizationSettings?> GetSettingsAsync()
    {
        var path = Path.Combine(_rootPath, SettingsFileName);
        if (!File.Exists(path))
            return Task.FromResult<UICustomizationSettings?>(null);

        var encrypted = File.ReadAllText(path, Encoding.UTF8);
        var json = _cryptoService.DecryptText(encrypted);
        return Task.FromResult(_blobSerializer.DeserializeObject<UICustomizationSettings?>(json));
    }

    public Task SaveSettingsAsync(UICustomizationSettings settings)
    {
        var path = Path.Combine(_rootPath, SettingsFileName);
        var json = _blobSerializer.SerializeObject(settings);
        var encrypted = _cryptoService.EncryptText(json);
        File.WriteAllText(path, encrypted, Encoding.UTF8);
        return Task.CompletedTask;
    }
}
