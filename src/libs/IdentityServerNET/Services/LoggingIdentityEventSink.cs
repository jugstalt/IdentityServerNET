using IdentityServerNET.Abstractions.EventSinks;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

// Default IIdentityEventSink so security-relevant events (login success/failure, logout, consent,
// token issuance, ...) are never silently dropped. EventSinkProxy fans events out to every
// registered IIdentityEventSink, and previously none was registered by default - PersistAsync
// looped zero times and every event vanished. Logging via the app's existing Serilog pipeline gives
// a durable trail for free wherever Serilog is already configured to write (console today; file/
// Seq/Elastic/etc. by adding a sink there, with no code change needed here).
public class LoggingIdentityEventSink : IIdentityEventSink
{
    private readonly ILogger<LoggingIdentityEventSink> _logger;

    public LoggingIdentityEventSink(ILogger<LoggingIdentityEventSink> logger)
    {
        _logger = logger;
    }

    public Task PersistAsync(IdentityEvent evt)
    {
        var level = evt.EventType switch
        {
            IdentityEventTypes.Error => LogLevel.Error,
            IdentityEventTypes.Failure => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            level,
            "IdentityServer event {Category}/{Name} ({EventType}) user={Username} remoteIp={RemoteIpAddress}: {Message}",
            evt.Category, evt.Name, evt.EventType, evt.Username, evt.RemoteIpAddress, evt.Message);

        return Task.CompletedTask;
    }
}
