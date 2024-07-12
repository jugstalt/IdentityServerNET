using IdentityServer.Nova.Abstractions.DbContext;
using IdentityServer.Nova.Abstractions.Services;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext;

public class FileBlobResourceExportDb : FileBlobResourceDb, IExportResourceDbContext
{
    public FileBlobResourceExportDb(IOptions<ExportResourceDbContextConfiguration> options)
        : base(options)
    {

    }

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
