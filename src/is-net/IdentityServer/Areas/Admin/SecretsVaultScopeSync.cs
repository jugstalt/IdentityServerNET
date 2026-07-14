using IdentityServerNET.Abstractions.DbContext;
using IdentityServerNET.Models.IdentityServerWrappers;
using IdentityServerNET.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.Areas.Admin;

/// <summary>
/// Best-effort sync between a realm admin's Secrets Vault lockers and the shared, global
/// "secrets-vault" API resource's scope list — so a client-credentials client can be granted access to
/// a new locker without the system admin manually adding the scope.
///
/// Silently does nothing if the "secrets-vault" resource doesn't exist yet (the system admin hasn't set
/// it up) — browser-based retrieval never depends on it, only client-credentials access does. Never
/// throws: a sync failure must not block the primary locker create/delete operation.
/// </summary>
internal static class SecretsVaultScopeSync
{
    private const string SecretsVaultResourceName = "secrets-vault";

    public static Task GrantLockerScopeAsync(IResourceDbContext resourceDb, string lockerName)
        => SyncAsync(resourceDb, lockerName, grant: true);

    public static Task RevokeLockerScopeAsync(IResourceDbContext resourceDb, string lockerName)
        => SyncAsync(resourceDb, lockerName, grant: false);

    private static async Task SyncAsync(IResourceDbContext resourceDb, string lockerName, bool grant)
    {
        try
        {
            if (resourceDb is not IResourceDbContextModify modify)
                return;

            var resource = await resourceDb.FindApiResourceAsync(SecretsVaultResourceName);
            if (resource is null)
                return;

            var scopeName = $"{SecretsVaultResourceName}.{lockerName}";
            var scopes = new List<ScopeModel>(resource.Scopes ?? Array.Empty<ScopeModel>());
            var alreadyPresent = scopes.Any(s => s.Name == scopeName);

            if (grant == alreadyPresent)
                return;

            resource.Scopes = grant
                ? scopes.Append(new ScopeModel { Name = scopeName }).ToArray()
                : scopes.Where(s => s.Name != scopeName).ToArray();

            // The realm admin does not own the global "secrets-vault" resource — act as the system
            // (global) realm for this one, narrowly-scoped write, same as realm provisioning does.
            using (RealmScopeOverride.Begin(null))
            {
                await modify.UpdateApiResourceAsync(resource, new[] { "Scopes" });
            }
        }
        catch
        {
            // Best-effort: the locker/secret operation itself must still succeed. The system admin can
            // always add/remove the scope manually via Resources -> secrets-vault -> Scopes.
        }
    }
}
