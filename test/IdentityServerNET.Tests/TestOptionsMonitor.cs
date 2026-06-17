using System;
using Microsoft.Extensions.Options;

namespace IdentityServerNET.Tests;

/// <summary>
/// Minimal <see cref="IOptionsMonitor{TOptions}"/> implementation for unit tests that always
/// returns a fixed options instance.
/// </summary>
internal sealed class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    public TestOptionsMonitor(T value) => CurrentValue = value;

    public T CurrentValue { get; }

    public T Get(string? name) => CurrentValue;

    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
