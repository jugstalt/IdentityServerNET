using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Abstractions.Services;
using IdentityServerNET.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServerNET.Services;

public class RealmUserScope : IRealmUserScope
{
    private readonly IRealmContext _realmContext;
    private readonly IRealmDbContext _realmDb;

    public RealmUserScope(IRealmContext realmContext, IRealmDbContext realmDb)
    {
        _realmContext = realmContext;
        _realmDb = realmDb;
    }

    public async Task<IEnumerable<ApplicationUser>> FilterToCurrentRealmAsync(IEnumerable<ApplicationUser> users, CancellationToken cancellationToken)
    {
        if (users == null)
        {
            return Array.Empty<ApplicationUser>();
        }

        var realm = await _realmContext.GetCurrentRealmAsync(cancellationToken);

        if (realm != null)
        {
            var domains = new HashSet<string>(realm.Domains ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            return users.Where(u => u != null && domains.Contains(DomainOf(u))).ToArray();
        }

        // System admin: only users whose domain is owned by no realm (the global namespace).
        var ownedDomains = await AllRealmDomainsAsync(cancellationToken);
        return users.Where(u => u != null && !ownedDomains.Contains(DomainOf(u))).ToArray();
    }

    public async Task<string> ValidateUserInCurrentRealmAsync(string userName, CancellationToken cancellationToken)
    {
        var domain = DomainOf(userName);
        var realm = await _realmContext.GetCurrentRealmAsync(cancellationToken);

        if (realm != null)
        {
            var domains = new HashSet<string>(realm.Domains ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(domain) || !domains.Contains(domain))
            {
                return $"As administrator of realm '{realm.Name}' you can only create users in the realm's domains " +
                       $"({string.Join(", ", realm.Domains ?? Enumerable.Empty<string>())}).";
            }

            return null;
        }

        // System admin: the user's domain must not belong to any realm.
        if (!string.IsNullOrEmpty(domain))
        {
            var owner = await _realmDb.FindByDomainAsync(domain, cancellationToken);
            if (owner != null)
            {
                return $"Domain '{domain}' belongs to realm '{owner.Name}'. Create this user as that realm's administrator.";
            }
        }

        return null;
    }

    private async Task<HashSet<string>> AllRealmDomainsAsync(CancellationToken cancellationToken)
    {
        var realms = await _realmDb.GetAllAsync(cancellationToken);
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var realm in realms)
        {
            if (realm.Domains == null)
            {
                continue;
            }

            foreach (var domain in realm.Domains)
            {
                set.Add(domain);
            }
        }

        return set;
    }

    private static string DomainOf(ApplicationUser user)
        => DomainOf(user?.UserName ?? user?.Email);

    private static string DomainOf(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return "";
        }

        int at = userName.LastIndexOf('@');
        return at >= 0 && at < userName.Length - 1
            ? userName.Substring(at + 1).ToLowerInvariant()
            : "";
    }
}
