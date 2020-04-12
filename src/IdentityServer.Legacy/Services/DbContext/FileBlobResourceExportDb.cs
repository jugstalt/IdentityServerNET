using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IdentityServer.Legacy.Services.Serialize;

namespace IdentityServer.Legacy.Services.DbContext
{
    public class FileBlobResourceExportDb : FileBlobResourceDb, IExportResourceDbContext
    {
        public FileBlobResourceExportDb(IOptions<ExportResourceDbContextConfiguration> options)
            : base(options)
        {
            
        }

        #region IResourceDbContextExport

        public Task FlushDb()
        {
            foreach(var fi in new DirectoryInfo(_rootPath).GetFiles("*.api").ToArray())
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
}
