using IdentityServer.Legacy.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class FileBlobClientExportDb : FileBlobClientDb, IExportClientDbContext
    {
        public FileBlobClientExportDb(IOptions<ExportClientDbContextConfiguration> options)
            : base(options)
        {
        }

        #region IClientDbContextExport

        public Task FlushDb()
        {
            foreach (var fi in new DirectoryInfo(_rootPath).GetFiles("*.client").ToArray())
            {
                fi.Delete();
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
