using IdentityServer4.Models;
using System;
using System.Collections.Generic;
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

    // Standard OIDC scopes / identity resources that are global by definition and must never be
    // realm-namespaced — every realm's clients rely on them.
    private static readonly HashSet<string> _globalReservedNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "openid", "profile", "email", "address", "phone", "offline_access", "roles",
        };

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

    /// <summary>
    /// Returns <c>true</c> if the (local) name is a global reserved name — a standard OIDC scope /
    /// identity resource (openid, profile, …) that must never be realm-namespaced.
    /// </summary>
    public static bool IsGlobalReservedName(this string? value)
        => value is not null && _globalReservedNames.Contains(value.GetRealmScopedName());

    /// <summary>
    /// Whether a user of realm <paramref name="userRealm"/> may use the client with the given id.
    /// A global client (no realm suffix) is usable by everyone; a realm client only by users of that
    /// same realm. This is the runtime cross-realm guard.
    /// </summary>
    public static bool ClientAllowsUserRealm(this string? clientId, string? userRealm)
    {
        string? clientRealm = clientId.GetRealm();

        return clientRealm is null
            || string.Equals(clientRealm, userRealm, StringComparison.Ordinal);
    }

    /// <summary>IS4 Client.Properties key carrying the extra allowed user e-mail domains.</summary>
    public const string AllowedUserDomainsProperty = "allowed_user_domains";

    /// <summary>
    /// Whether the user (realm + e-mail domain) may use this client. Extends the realm rule
    /// (<see cref="ClientAllowsUserRealm"/>) by the per-client AllowedUserDomains list
    /// (stored in <see cref="AllowedUserDomainsProperty"/>, space/comma/semicolon separated,
    /// <c>*</c> allows everyone).
    /// </summary>
    public static bool ClientAllowsUser(this Client? client, string? userRealm, string? userEmailDomain)
    {
        if (client is null)
            return true;

        if (client.ClientId.ClientAllowsUserRealm(userRealm))
            return true;

        if (client.Properties is not null &&
            client.Properties.TryGetValue(AllowedUserDomainsProperty, out var domains) &&
            !string.IsNullOrEmpty(domains))
        {
            foreach (var d in domains.Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (d == "*" || string.Equals(d, userEmailDomain, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }
}
