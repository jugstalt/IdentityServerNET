using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServerNET.Abstractions.EventSinks;
using IdentityServerNET.Services;
using Microsoft.Extensions.Logging;
using Xunit;

namespace IdentityServerNET.Tests;

/// <summary>
/// Tests for <see cref="LoggingIdentityEventSink"/>, the default <see cref="IIdentityEventSink"/>.
/// Before this existed, no IIdentityEventSink was registered anywhere by default, so EventSinkProxy
/// iterated an empty list and every security event (login success/failure, logout, consent, ...) was
/// generated and immediately dropped - there was no audit trail at all out of the box.
/// </summary>
public class LoggingIdentityEventSinkTests
{
    private sealed class FakeLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, System.Exception? exception, Func<TState, System.Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose() { }
        }
    }

    [Theory]
    [InlineData(IdentityEventTypes.Success, LogLevel.Information)]
    [InlineData(IdentityEventTypes.Information, LogLevel.Information)]
    [InlineData(IdentityEventTypes.Failure, LogLevel.Warning)]
    [InlineData(IdentityEventTypes.Error, LogLevel.Error)]
    public async Task PersistAsync_MapsEventTypeToExpectedLogLevel(IdentityEventTypes eventType, LogLevel expectedLevel)
    {
        var logger = new FakeLogger<LoggingIdentityEventSink>();
        var sink = new LoggingIdentityEventSink(logger);

        await sink.PersistAsync(new IdentityEvent
        {
            EventType = eventType,
            Category = "Authentication",
            Name = "UserLogin",
            Username = "alice",
            Message = "test event"
        });

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(expectedLevel, entry.Level);
    }

    [Fact]
    public async Task PersistAsync_IncludesKeyFieldsInTheLoggedMessage()
    {
        var logger = new FakeLogger<LoggingIdentityEventSink>();
        var sink = new LoggingIdentityEventSink(logger);

        await sink.PersistAsync(new IdentityEvent
        {
            EventType = IdentityEventTypes.Failure,
            Category = "Authentication",
            Name = "UserLoginFailure",
            Username = "alice@example.com",
            RemoteIpAddress = "203.0.113.5",
            Message = "invalid credentials"
        });

        var message = logger.Entries.Single().Message;
        Assert.Contains("Authentication", message);
        Assert.Contains("UserLoginFailure", message);
        Assert.Contains("alice@example.com", message);
        Assert.Contains("203.0.113.5", message);
        Assert.Contains("invalid credentials", message);
    }
}
