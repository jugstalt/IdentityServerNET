using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.DbContext;

public interface IExportClientDbContext : IClientDbContextModify
{
    Task FlushDb();
}
