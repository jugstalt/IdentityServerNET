using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.DbContext
{
    public interface IExportClientDbContext : IClientDbContextModify
    {
        Task FlushDb();
    }
}
