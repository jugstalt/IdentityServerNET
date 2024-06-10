using System.Threading.Tasks;

namespace IdentityServer.Nova.Services.EventSinks
{
    public interface IIdentityEventSink
    {
        Task PersistAsync(IdentityEvent evt);
    }
}
