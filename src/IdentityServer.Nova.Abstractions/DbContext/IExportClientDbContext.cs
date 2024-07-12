using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.DbContext;

public interface IExportClientDbContext : IClientDbContextModify
{
    Task FlushDb();
}
