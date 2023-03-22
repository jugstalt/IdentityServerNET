using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IExportResourceDbContext : IResourceDbContextModify
    {
        Task FlushDb();
    }
}
