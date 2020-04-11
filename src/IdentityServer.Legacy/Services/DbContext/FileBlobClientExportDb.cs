using IdentityServer.Legacy.Services.Cryptography;
using IdentityServer.Legacy.DependencyInjection;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
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
