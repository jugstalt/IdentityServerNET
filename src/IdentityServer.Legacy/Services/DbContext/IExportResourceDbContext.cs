using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IExportResourceDbContext : IResourceDbContextModify
    {
        Task FlushDb();
    }
}
