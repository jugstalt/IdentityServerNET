using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.DbContext;

public interface IExportResourceDbContext : IResourceDbContextModify
{
    Task FlushDb();
}
