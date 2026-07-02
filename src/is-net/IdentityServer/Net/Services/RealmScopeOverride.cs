#nullable enable
using System;
using System.Threading;

namespace IdentityServerNET.Services;

/// <summary>
/// Temporarily overrides the current realm for the current async flow. Used by privileged
/// operations — realm provisioning / deprovisioning — that must act as a specific realm from the
/// system-admin context (where the store decorators would otherwise strip/deny realm-scoped items).
///
/// The override is read by <see cref="RealmContext"/>, which the DbContext decorators consult, so a
/// <c>using (RealmScopeOverride.Begin("xyz")) { ... }</c> block makes every store operation inside it
/// behave as realm <c>xyz</c>, regardless of the logged-in user.
/// </summary>
public static class RealmScopeOverride
{
    private sealed class Entry
    {
        public string? Realm;
    }

    private static readonly AsyncLocal<Entry?> _current = new();

    /// <summary>Whether an override is currently active (a null realm means "global" explicitly).</summary>
    public static bool IsActive => _current.Value is not null;

    /// <summary>The overridden realm (may be null for the global namespace) when <see cref="IsActive"/>.</summary>
    public static string? Current => _current.Value?.Realm;

    public static IDisposable Begin(string? realmName)
    {
        var previous = _current.Value;
        _current.Value = new Entry { Realm = realmName };
        return new Scope(() => _current.Value = previous);
    }

    private sealed class Scope : IDisposable
    {
        private Action? _dispose;
        public Scope(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}
