using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.EventSinks
{
    public interface IIdentityEventSink
    {
        Task PersistAsync(IdentityEvent evt);
    }
}
