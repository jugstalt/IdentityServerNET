using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.EventSinks;

public interface IIdentityEventSink
{
    Task PersistAsync(IdentityEvent evt);
}
