using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.EventSinks;

public interface IIdentityEventSink
{
    Task PersistAsync(IdentityEvent evt);
}
