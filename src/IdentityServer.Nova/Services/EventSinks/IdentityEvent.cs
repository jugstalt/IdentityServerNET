using Newtonsoft.Json;
using System;

namespace IdentityServer.Nova.Services.EventSinks;

public class IdentityEvent
{
    public IdentityEvent()
    {
    }

    [JsonProperty(Order = -99)]
    public string Category { get; set; }


    [JsonProperty(Order = -100)]
    public string Name { get; set; }

    [JsonProperty(Order = -98)]
    public IdentityEventTypes EventType { get; set; }

    [JsonProperty(Order = -97)]
    public int Id { get; set; }

    public string Message { get; set; }

    public string ActivityId { get; set; }

    public DateTime TimeStamp { get; set; }

    public int ProcessId { get; set; }

    public string LocalIpAddress { get; set; }

    public string RemoteIpAddress { get; set; }

    public string Username { get; set; }
}
