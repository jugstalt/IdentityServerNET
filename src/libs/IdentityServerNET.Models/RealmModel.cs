using System;
using System.Collections.Generic;

namespace IdentityServerNET.Models;

/// <summary>
/// A realm groups one or more user e-mail domains into an isolated tenant. Clients, roles and
/// resources of a realm carry the <c>@{Name}</c> suffix (see
/// <see cref="Extensions.RealmConventionExtensions"/>); users belong to a realm through their
/// e-mail domain, which must be one of <see cref="Domains"/>.
///
/// A domain may belong to at most one realm (enforced by the storage layer).
/// </summary>
public class RealmModel
{
    /// <summary>
    /// The realm slug (lowercase namespace, e.g. "xyz"). Serves both as the identifier and as the
    /// <c>@realm</c> suffix appended to realm-scoped client/role/resource ids.
    /// </summary>
    public string Name { get; set; } = "";

    public string DisplayName { get; set; } = "";

    /// <summary>
    /// The domain that anchors the realm admin (<c>admin@{PrimaryDomain}</c>). Must be contained in
    /// <see cref="Domains"/>.
    /// </summary>
    public string PrimaryDomain { get; set; } = "";

    /// <summary>All user e-mail domains that belong to this realm. Always includes <see cref="PrimaryDomain"/>.</summary>
    public ICollection<string> Domains { get; set; } = new List<string>();

    public DateTime CreateDate { get; set; }
}
