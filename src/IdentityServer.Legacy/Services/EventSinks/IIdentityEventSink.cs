using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.EventSinks
{
    public interface IIdentityEventSink
    {
        Task PersistAsync(IdentityEvent evt);
    }
}
