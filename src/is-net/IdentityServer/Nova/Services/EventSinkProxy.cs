using IdentityServerNET.Abstractions.EventSinks;
using IdentityServer4.Events;
using IdentityServer4.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class EventSinkProxy : IEventSink
{
    private IEnumerable<IIdentityEventSink> _identityEventSinks;

    public EventSinkProxy(IEnumerable<IIdentityEventSink> identityEventSinks)
    {
        _identityEventSinks = identityEventSinks;
    }

    #region IEventSink 

    async public Task PersistAsync(Event evt)
    {
        if (_identityEventSinks == null)
        {
            return;
        }

        string username = String.Empty;
        if (evt is UserLoginFailureEvent)
        {
            username = ((UserLoginFailureEvent)evt).Username;
        }
        else if (evt is UserLoginSuccessEvent)
        {
            username = ((UserLoginSuccessEvent)evt).Username;
        }
        else if (evt is UserLogoutSuccessEvent)
        {
            username = ((UserLogoutSuccessEvent)evt).DisplayName;
        }

        foreach (var identityEventSink in _identityEventSinks)
        {
            await identityEventSink.PersistAsync(new IdentityEvent()
            {
                Category = evt.Category,
                Name = evt.Name,
                EventType = (IdentityEventTypes)(int)evt.EventType,
                Id = evt.Id,
                Message = evt.Message,
                LocalIpAddress = evt.LocalIpAddress,
                ProcessId = evt.ProcessId,
                ActivityId = evt.ActivityId,
                RemoteIpAddress = evt.RemoteIpAddress,
                TimeStamp = evt.TimeStamp,
                Username = username
            });
        }
    }

    #endregion
}
