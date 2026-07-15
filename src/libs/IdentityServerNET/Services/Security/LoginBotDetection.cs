using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.Security;

public class LoginBotDetection : ILoginBotDetection
{
    // Distinct prefixes keep this feature's entries identifiable and collision-free in a shared
    // IDistributedCache backend (e.g. Redis, also used by the PAR / authorization-parameter stores).
    private const string UsernameKeyPrefix = "loginbotdetection:user:";
    private const string IpKeyPrefix = "loginbotdetection:ip:";

    // IDistributedCache has no key-enumeration API (Redis SCAN, SQL Server, in-memory all differ), so
    // the set of currently-tracked usernames/IPs is maintained separately here - purely to let the admin
    // UI (see Areas/Admin/Pages/BotDetection) list and clear entries without needing backend-specific code.
    private const string UsernameIndexKey = "loginbotdetection:index:usernames";
    private const string IpIndexKey = "loginbotdetection:index:ips";

    private readonly IDistributedCache _cache;
    private readonly LoginBotDetectionOptions _options;

    public LoginBotDetection(IDistributedCache cache, IOptionsMonitor<LoginBotDetectionOptions> options)
    {
        _cache = cache;
        _options = options.CurrentValue ?? new LoginBotDetectionOptions();
    }

    #region ILoginBotDetection

    async public Task<bool> IsSuspiciousUserAsync(string username)
    {
        if (String.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username required");
        }

        var normalized = NormalizeUsername(username);
        var suspiciousUser = SuspiciousEntry.FromString(await _cache.GetStringAsync(UsernameKeyPrefix + normalized));

        if (suspiciousUser != null)
        {
            var lastSet = suspiciousUser.TimeStamp.ToUniversalTime();
            if ((DateTime.UtcNow - lastSet).TotalMinutes >= _options.RembemberSuspiciousUserTotalMinutes)
            {
                await RemoveSuspiciousUserAsync(username);
                return false;
            }

            return suspiciousUser.CountFailes >= _options.MaxFailCount;
        }

        return false;
    }

    async public Task AddSuspiciousUserAsync(string username)
    {
        var normalized = NormalizeUsername(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(UsernameKeyPrefix + normalized));

        suspiciousUser.TimeStamp = DateTime.Now;
        suspiciousUser.CountFailes++;

        await SaveUsernameEntryAsync(normalized, suspiciousUser);
    }

    async public Task RemoveSuspiciousUserAsync(string username)
    {
        var normalized = NormalizeUsername(username);
        await _cache.RemoveAsync(UsernameKeyPrefix + normalized);
        await RemoveFromIndexAsync(UsernameIndexKey, normalized);
    }

    async public Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username)
    {
        var normalized = NormalizeUsername(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(UsernameKeyPrefix + normalized));
        suspiciousUser.CountFailes++;

        string code = GenerateCaptchaCode();

        suspiciousUser.CaptchaCode = code;
        suspiciousUser.TimeStamp = DateTime.Now;

        await SaveUsernameEntryAsync(normalized, suspiciousUser);

        return code;
    }

    // Re-displaying the password form (e.g. the user navigates back and re-enters their username, or
    // just reloads the page) must still show a CAPTCHA if they're already suspicious - otherwise the
    // POST handler demands a CaptchaCode the user was never shown, and rejects even a correct password.
    // Unlike AddSuspicousUserAndGenerateCaptchaCodeAsync, this must NOT count as a failure: merely
    // viewing the page is not a failed login attempt, so CountFailes/TimeStamp are left untouched, and
    // the username is deliberately NOT added to the admin-facing index - only actual failures are.
    async public Task<string> EnsureCaptchaCodeAsync(string username)
    {
        var key = UsernameKeyPrefix + NormalizeUsername(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(key));

        var code = GenerateCaptchaCode();
        suspiciousUser.CaptchaCode = code;

        // Not indexed (see class-level note on EnsureCaptchaCodeAsync) and CountFailes may be 0 here (a
        // username that's only suspicious because its IP is), so nothing else would ever prune this
        // entry - give it a real TTL instead of relying on someone re-checking this exact username later.
        await _cache.SetStringAsync(key, suspiciousUser.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CacheTtl(_options.RembemberSuspiciousUserTotalMinutes)
        });

        return code;
    }

    async public Task<bool> VerifyCaptchaCodeAsync(string username, string code)
    {
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(UsernameKeyPrefix + NormalizeUsername(username)));

        return code != null && code.Equals(suspiciousUser.CaptchaCode, StringComparison.InvariantCultureIgnoreCase);
    }

    private string GenerateCaptchaCode()
    {
        Random rand = new Random();
        int maxRand = _options.CaptchaCodeLetters.Length - 1;

        StringBuilder sb = new StringBuilder();

        for (int i = 0; i < _options.CaptchaCodeLength; i++)
        {
            int index = rand.Next(maxRand);
            sb.Append(_options.CaptchaCodeLetters[index]);
        }

        return sb.ToString();
    }

    // Keyed by the same normalized username as the rest of this class (see NormalizeUsername) so the
    // tar-pit delay can't be bypassed the same way the fail-counter could.
    private static readonly ConcurrentDictionary<string, DateTime> _suspiciousUserBlocks = new();

    async public Task BlockSuspicousUser(string username)
    {
        var key = UsernameKeyPrefix + NormalizeUsername(username);

        // TryGetValue instead of ContainsKey+indexer: those are two separate, non-atomic operations on
        // a ConcurrentDictionary - a concurrent call finishing its delay and removing the entry between
        // them would otherwise throw KeyNotFoundException on the indexer access.
        if (_suspiciousUserBlocks.TryGetValue(key, out var blockedAt) &&
            (DateTime.UtcNow - blockedAt).TotalSeconds < _options.BlockSuspiciousUserSeconds)
        {
            throw new StatusMessageException("Suspicous bot request detected");
        }

        _suspiciousUserBlocks[key] = DateTime.UtcNow;
        await Task.Delay(_options.BlockSuspiciousUserSeconds * 1000);
        _suspiciousUserBlocks.TryRemove(key, out _);
    }

    // IP-based tracking is deliberately weaker than username tracking: an IP can legitimately represent
    // many unrelated users behind NAT, a corporate proxy, or a mobile carrier, so it must only ever add
    // friction (forcing a CAPTCHA - see AccountController.LoginPassword), never trigger BlockSuspicousUser's
    // tar-pit delay, which would collaterally stall every other user sharing that IP.
    //
    // Unlike the username counter, this is intentionally never cleared on a successful sign-in: a
    // credential-stuffing/password-spraying run is mostly failures with the occasional deliberate hit, and
    // clearing on any success would let that occasional hit reset the counter and defeat the detection. It
    // only decays via RememberSuspiciousIpTotalMinutes, or an explicit admin RemoveSuspiciousIpAsync call.
    async public Task<bool> IsSuspiciousIpAsync(string ipAddress)
    {
        if (String.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address required");
        }

        var normalized = NormalizeIp(ipAddress);
        var suspiciousIp = SuspiciousEntry.FromString(await _cache.GetStringAsync(IpKeyPrefix + normalized));

        if (suspiciousIp != null)
        {
            var lastSet = suspiciousIp.TimeStamp.ToUniversalTime();
            if ((DateTime.UtcNow - lastSet).TotalMinutes >= _options.RememberSuspiciousIpTotalMinutes)
            {
                await RemoveSuspiciousIpAsync(ipAddress);
                return false;
            }

            return suspiciousIp.CountFailes >= _options.MaxIpFailCount;
        }

        return false;
    }

    async public Task AddSuspiciousIpAsync(string ipAddress)
    {
        var normalized = NormalizeIp(ipAddress);
        var suspiciousIp = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(IpKeyPrefix + normalized));

        suspiciousIp.TimeStamp = DateTime.Now;
        suspiciousIp.CountFailes++;

        await SaveIpEntryAsync(normalized, suspiciousIp);
    }

    // Only meant for deliberate admin use (see Areas/Admin/Pages/BotDetection) - never called from the
    // login flow itself, which would otherwise let an occasional successful credential-stuffing hit
    // reset the counter and defeat the detection (see IsSuspiciousIpAsync above).
    async public Task RemoveSuspiciousIpAsync(string ipAddress)
    {
        var normalized = NormalizeIp(ipAddress);
        await _cache.RemoveAsync(IpKeyPrefix + normalized);
        await RemoveFromIndexAsync(IpIndexKey, normalized);
    }

    async public Task<IReadOnlyCollection<LoginBotDetectionEntry>> GetSuspiciousUsersAsync()
    {
        return await ListEntriesAsync(
            UsernameIndexKey,
            UsernameKeyPrefix,
            _options.RembemberSuspiciousUserTotalMinutes,
            _options.MaxFailCount);
    }

    async public Task<IReadOnlyCollection<LoginBotDetectionEntry>> GetSuspiciousIpsAsync()
    {
        return await ListEntriesAsync(
            IpIndexKey,
            IpKeyPrefix,
            _options.RememberSuspiciousIpTotalMinutes,
            _options.MaxIpFailCount);
    }

    #endregion

    #region Helpers

    // Case-varying the submitted username (e.g. "Admin@is.net" vs "admin@is.net") must not create a
    // fresh, independent fail-counter - otherwise the fail-counter/tar-pit is trivially bypassed by an
    // attacker cycling through casings, and a legitimate user's own case-inconsistent typing keeps
    // stale "suspicious" entries alive under variants that never get cleared on success.
    private static string NormalizeUsername(string username) => username.Trim().ToUpperInvariant();

    private static string NormalizeIp(string ipAddress) => ipAddress.Trim();

    private async Task SaveUsernameEntryAsync(string normalizedUsername, SuspiciousEntry entry)
    {
        await _cache.SetStringAsync(
            UsernameKeyPrefix + normalizedUsername,
            entry.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl(_options.RembemberSuspiciousUserTotalMinutes) });
        await AddToIndexAsync(UsernameIndexKey, normalizedUsername);
    }

    private async Task SaveIpEntryAsync(string normalizedIp, SuspiciousEntry entry)
    {
        await _cache.SetStringAsync(
            IpKeyPrefix + normalizedIp,
            entry.ToString(),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheTtl(_options.RememberSuspiciousIpTotalMinutes) });
        await AddToIndexAsync(IpIndexKey, normalizedIp);
    }

    // A configured remember-window of 0 (or, in theory, negative) minutes is a valid "treat as stale
    // immediately" setting for the app-level staleness check above - but IDistributedCache implementations
    // reject a non-positive AbsoluteExpirationRelativeToNow outright, so the cache TTL itself always needs
    // at least a minimal positive floor regardless of what the configured window says.
    private static TimeSpan CacheTtl(int rememberMinutes) => TimeSpan.FromMinutes(Math.Max(rememberMinutes, 1));

    // Reads every key currently listed in the given index, drops the ones that expired or were removed
    // (pruning the index as it goes, so stale entries don't accumulate forever if nobody ever clears
    // them through the admin UI), and returns the rest as display-ready entries.
    private async Task<IReadOnlyCollection<LoginBotDetectionEntry>> ListEntriesAsync(
        string indexKey, string keyPrefix, int rememberMinutes, int maxFailCount)
    {
        var tracked = await LoadIndexAsync(indexKey);
        if (tracked.Count == 0)
        {
            return Array.Empty<LoginBotDetectionEntry>();
        }

        var result = new List<LoginBotDetectionEntry>();
        var stale = new List<string>();

        foreach (var normalizedKey in tracked)
        {
            var entry = SuspiciousEntry.FromString(await _cache.GetStringAsync(keyPrefix + normalizedKey));
            if (entry == null)
            {
                stale.Add(normalizedKey);
                continue;
            }

            var lastFailureUtc = entry.TimeStamp.ToUniversalTime();
            if ((DateTime.UtcNow - lastFailureUtc).TotalMinutes >= rememberMinutes)
            {
                await _cache.RemoveAsync(keyPrefix + normalizedKey);
                stale.Add(normalizedKey);
                continue;
            }

            result.Add(new LoginBotDetectionEntry
            {
                Key = normalizedKey,
                FailCount = entry.CountFailes,
                LastFailureUtc = lastFailureUtc,
                IsSuspicious = entry.CountFailes >= maxFailCount
            });
        }

        if (stale.Count > 0)
        {
            await RemoveFromIndexAsync(indexKey, stale);
        }

        return result.OrderByDescending(e => e.LastFailureUtc).ToList();
    }

    private async Task<HashSet<string>> LoadIndexAsync(string indexKey)
    {
        var json = await _cache.GetStringAsync(indexKey);
        if (String.IsNullOrEmpty(json))
        {
            return new HashSet<string>();
        }

        return JsonConvert.DeserializeObject<HashSet<string>>(json) ?? new HashSet<string>();
    }

    // Best-effort, non-atomic read-modify-write, same as the rest of this class - a lost concurrent
    // update just means an entry is briefly missing from (or lingers in) the admin list, not a security
    // regression, since the authoritative suspicious/not-suspicious decision always reads the entry
    // itself (see IsSuspiciousUserAsync/IsSuspiciousIpAsync), never the index.
    private async Task AddToIndexAsync(string indexKey, string normalizedValue)
    {
        var set = await LoadIndexAsync(indexKey);
        if (set.Add(normalizedValue))
        {
            await _cache.SetStringAsync(indexKey, JsonConvert.SerializeObject(set));
        }
    }

    private Task RemoveFromIndexAsync(string indexKey, string normalizedValue) =>
        RemoveFromIndexAsync(indexKey, new[] { normalizedValue });

    private async Task RemoveFromIndexAsync(string indexKey, IEnumerable<string> normalizedValues)
    {
        var set = await LoadIndexAsync(indexKey);
        var changed = false;
        foreach (var value in normalizedValues)
        {
            changed |= set.Remove(value);
        }

        if (changed)
        {
            await _cache.SetStringAsync(indexKey, JsonConvert.SerializeObject(set));
        }
    }

    #endregion

    #region Classes

    private class SuspiciousEntry
    {
        [JsonProperty("ts")]
        public DateTime TimeStamp { get; set; } = DateTime.Now;
        [JsonProperty("cc")]
        public string CaptchaCode { get; set; }
        [JsonProperty("cf")]
        public int CountFailes { get; set; }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        static public SuspiciousEntry FromString(string json)
        {
            if (String.IsNullOrEmpty(json))
            {
                return null;
            }

            return JsonConvert.DeserializeObject<SuspiciousEntry>(json);
        }

        static public SuspiciousEntry FromStringOrDefault(string json)
        {
            if (String.IsNullOrEmpty(json))
            {
                return new SuspiciousEntry();
            }

            return JsonConvert.DeserializeObject<SuspiciousEntry>(json);
        }
    }

    #endregion
}
