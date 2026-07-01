using System;
using System.Text.RegularExpressions;

namespace IdentityServerNET.Models.Extensions;

/// <summary>
/// Single source of truth for the realm naming convention used by realm-scoped entities
/// (clients, roles, resources). The convention is <c>{name}{RealmSeparator}{realm}</c>,
/// e.g. <c>my-client@xyz</c>.
///
/// Users do NOT follow this convention — a user's realm is derived from its e-mail domain,
/// never from a suffix on the identifier.
///
/// Everything about the convention (the separator, how a realm is parsed, how a name is
/// composed) is contained in this one class, so the approach can be changed — or abandoned —
/// in exactly one place.
/// </summary>
public static class RealmConventionExtensions
{
    /// <summary>The character separating the local name from the realm slug.</summary>
    public const char RealmSeparator = '@';

    // A realm slug has the same shape as a namespace: lowercase letters, digits and dashes.
    // Crucially it never contains a dot, which is what keeps a realm-scoped id (my-client@xyz)
    // distinguishable from an e-mail-shaped value (user@foo.com).
    private static readonly Regex _realmSlugShape =
        new(@"^[a-z0-9\-]+$", RegexOptions.Compiled);

    /// <summary>
    /// Returns <c>true</c> if the identifier carries a realm namespace (e.g. <c>my-client@xyz</c>).
    /// Returns <c>false</c> for global identifiers (<c>my-client</c>) and for e-mail-shaped values
    /// (<c>user@foo.com</c>), because a realm slug never contains a dot.
    /// </summary>
    public static bool HasRealm(this string? value)
        => value.GetRealm() is not null;

    /// <summary>
    /// Extracts the realm slug from an identifier, or <c>null</c> if it carries none.
    /// </summary>
    public static string? GetRealm(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return null;
        }

        int idx = value!.LastIndexOf(RealmSeparator);
        if (idx <= 0 || idx == value.Length - 1)
        {
            // no separator, nothing before it, or nothing after it
            return null;
        }

        string suffix = value.Substring(idx + 1);

        return _realmSlugShape.IsMatch(suffix) ? suffix : null;
    }

    /// <summary>
    /// Returns the local name without the realm namespace. For an identifier without a realm
    /// the original value is returned unchanged.
    /// </summary>
    public static string GetRealmScopedName(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (value.GetRealm() is null)
        {
            return value!;
        }

        int idx = value!.LastIndexOf(RealmSeparator);
        return value.Substring(0, idx);
    }

    /// <summary>
    /// Composes a realm-scoped identifier <c>{name}{RealmSeparator}{realm}</c>. If
    /// <paramref name="realm"/> is null/empty the bare name is returned (global namespace).
    /// Any realm already present on <paramref name="name"/> is stripped first, so the operation
    /// is idempotent and never produces a double suffix.
    /// </summary>
    public static string AddRealmNamespace(this string name, string? realm)
    {
        if (name is null)
        {
            throw new ArgumentNullException(nameof(name));
        }

        string localName = name.GetRealmScopedName();

        return string.IsNullOrEmpty(realm)
            ? localName
            : $"{localName}{RealmSeparator}{realm}";
    }

    /// <summary>
    /// Removes the realm namespace from an identifier (alias of <see cref="GetRealmScopedName"/>).
    /// </summary>
    public static string RemoveRealmNamespace(this string? value)
        => value.GetRealmScopedName();

    /// <summary>
    /// Returns <c>true</c> if the identifier belongs to the given realm. A null/empty
    /// <paramref name="realm"/> denotes the global namespace, which matches identifiers without a realm.
    /// </summary>
    public static bool BelongsToRealm(this string? value, string? realm)
    {
        string? valueRealm = value.GetRealm();

        return string.IsNullOrEmpty(realm)
            ? valueRealm is null
            : string.Equals(valueRealm, realm, StringComparison.Ordinal);
    }
}
