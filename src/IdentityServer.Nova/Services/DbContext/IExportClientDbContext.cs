using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.DbContext;

public interface IExportClientDbContext : IClientDbContextModify
{
    Task FlushDb();
}
