using System;
using System.Collections.Generic;
using System.Linq;

namespace IdentityServerNET.Models.Extensions;

public static class RealmModelExtensions
{
    /// <summary>
    /// Normalises the realm in place (lower-cases and trims name and domains, de-duplicates domains,
    /// ensures the primary domain is part of the domain set) and validates its structure.
    /// Throws <see cref="ArgumentException"/> when the realm is not well-formed.
    /// </summary>
    public static RealmModel NormalizeAndValidate(this RealmModel realm)
    {
        if (realm is null)
        {
            throw new ArgumentNullException(nameof(realm));
        }

        realm.Name = (realm.Name ?? "").Trim().ToLowerInvariant();
        realm.PrimaryDomain = (realm.PrimaryDomain ?? "").Trim().ToLowerInvariant();
        realm.DisplayName = string.IsNullOrWhiteSpace(realm.DisplayName) ? realm.Name : realm.DisplayName.Trim();

        var domains = (realm.Domains ?? new List<string>())
            .Where(d => !string.IsNullOrWhiteSpace(d))
            .Select(d => d.Trim().ToLowerInvariant())
            .Distinct()
            .ToList();

        if (!string.IsNullOrEmpty(realm.PrimaryDomain) && !domains.Contains(realm.PrimaryDomain))
        {
            domains.Insert(0, realm.PrimaryDomain);
        }

        realm.Domains = domains;

        if (!realm.Name.IsValidRealmName())
        {
            throw new ArgumentException(
                $"Invalid realm name '{realm.Name}'. Expected a lowercase slug (letters, digits, single dashes, min. 3 chars).");
        }

        if (domains.Count == 0)
        {
            throw new ArgumentException($"Realm '{realm.Name}' must define at least one domain.");
        }

        if (string.IsNullOrEmpty(realm.PrimaryDomain))
        {
            throw new ArgumentException($"Realm '{realm.Name}' must define a primary domain.");
        }

        foreach (var domain in domains)
        {
            if (!domain.IsValidUserDomain())
            {
                throw new ArgumentException($"Invalid domain '{domain}' in realm '{realm.Name}'.");
            }
        }

        return realm;
    }

    /// <summary>
    /// Minimal shape check for a user e-mail domain: non-empty, contains a dot, and no characters
    /// that would break the <c>user@domain</c> / <c>name@realm</c> conventions.
    /// </summary>
    public static bool IsValidUserDomain(this string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        if (domain.IndexOf(RealmConventionExtensions.RealmSeparator) >= 0 ||
            domain.Any(char.IsWhiteSpace))
        {
            return false;
        }

        int dot = domain.IndexOf('.');
        return dot > 0 && dot < domain.Length - 1;
    }
}
