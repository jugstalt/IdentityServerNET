using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Services.Cryptography;
using IdentityServerNET.Services.Serialize;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.DbContext;

public class FileBlobResourceExportDb : FileBlobResourceDb, IExportResourceDbContext
{
    public FileBlobResourceExportDb(
            IOptions<ExportResourceDbContextConfiguration> options
        )
        : base(options,
               new ClearTextCryptoService(),
               new JsonBlobSerializer()
               {
                   JsonFormatting = Newtonsoft.Json.Formatting.Indented
               })
    { }

    #region IResourceDbContextExport

    public Task FlushDb()
    {
        foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.api").ToArray())
        {
            fi.Delete();
        }

        foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.identity").ToArray())
        {
            fi.Delete();
        }

        return Task.CompletedTask;
    }

    #endregion
}
