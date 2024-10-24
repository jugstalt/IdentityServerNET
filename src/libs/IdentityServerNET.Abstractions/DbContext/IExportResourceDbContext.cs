using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IExportResourceDbContext : IResourceDbContextModify
{
    Task FlushDb();
}
