using IdentityServerNET.Abstractions.Security;
using IdentityServerNET.Exceptions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServerNET.Services.Security;

public class LoginBotDetection : ILoginBotDetection
{
    // Distinct prefixes keep this feature's entries identifiable and collision-free in a shared
    // IDistributedCache backend (e.g. Redis, also used by the PAR / authorization-parameter stores).
    private const string UsernameKeyPrefix = "loginbotdetection:user:";
    private const string IpKeyPrefix = "loginbotdetection:ip:";

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

        var key = UsernameCacheKey(username);
        var suspiciousUser = SuspiciousEntry.FromString(await _cache.GetStringAsync(key));

        if (suspiciousUser != null)
        {
            var lastSet = suspiciousUser.TimeStamp.ToUniversalTime();
            if ((DateTime.UtcNow - lastSet).TotalMinutes >= _options.RembemberSuspiciousUserTotalMinutes)
            {
                await _cache.RemoveAsync(key);
                return false;
            }

            return suspiciousUser.CountFailes >= _options.MaxFailCount;
        }

        return false;
    }

    async public Task AddSuspiciousUserAsync(string username)
    {
        var key = UsernameCacheKey(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(key));

        suspiciousUser.TimeStamp = DateTime.Now;
        suspiciousUser.CountFailes++;

        await _cache.SetStringAsync(key, suspiciousUser.ToString());
    }

    async public Task RemoveSuspiciousUserAsync(string username)
    {
        await _cache.RemoveAsync(UsernameCacheKey(username));
    }

    async public Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username)
    {
        var key = UsernameCacheKey(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(key));
        suspiciousUser.CountFailes++;

        string code = GenerateCaptchaCode();

        suspiciousUser.CaptchaCode = code;
        suspiciousUser.TimeStamp = DateTime.Now;

        await _cache.SetStringAsync(key, suspiciousUser.ToString());

        return code;
    }

    // Re-displaying the password form (e.g. the user navigates back and re-enters their username, or
    // just reloads the page) must still show a CAPTCHA if they're already suspicious - otherwise the
    // POST handler demands a CaptchaCode the user was never shown, and rejects even a correct password.
    // Unlike AddSuspicousUserAndGenerateCaptchaCodeAsync, this must NOT count as a failure: merely
    // viewing the page is not a failed login attempt, so CountFailes/TimeStamp are left untouched.
    async public Task<string> EnsureCaptchaCodeAsync(string username)
    {
        var key = UsernameCacheKey(username);
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(key));

        var code = GenerateCaptchaCode();
        suspiciousUser.CaptchaCode = code;

        await _cache.SetStringAsync(key, suspiciousUser.ToString());

        return code;
    }

    async public Task<bool> VerifyCaptchaCodeAsync(string username, string code)
    {
        var suspiciousUser = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(UsernameCacheKey(username)));

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
        var key = UsernameCacheKey(username);

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
    // only decays via RememberSuspiciousIpTotalMinutes.
    async public Task<bool> IsSuspiciousIpAsync(string ipAddress)
    {
        if (String.IsNullOrWhiteSpace(ipAddress))
        {
            throw new ArgumentException("IP address required");
        }

        var key = IpCacheKey(ipAddress);
        var suspiciousIp = SuspiciousEntry.FromString(await _cache.GetStringAsync(key));

        if (suspiciousIp != null)
        {
            var lastSet = suspiciousIp.TimeStamp.ToUniversalTime();
            if ((DateTime.UtcNow - lastSet).TotalMinutes >= _options.RememberSuspiciousIpTotalMinutes)
            {
                await _cache.RemoveAsync(key);
                return false;
            }

            return suspiciousIp.CountFailes >= _options.MaxIpFailCount;
        }

        return false;
    }

    async public Task AddSuspiciousIpAsync(string ipAddress)
    {
        var key = IpCacheKey(ipAddress);
        var suspiciousIp = SuspiciousEntry.FromStringOrDefault(await _cache.GetStringAsync(key));

        suspiciousIp.TimeStamp = DateTime.Now;
        suspiciousIp.CountFailes++;

        await _cache.SetStringAsync(key, suspiciousIp.ToString());
    }

    #endregion

    #region Helpers

    // Case-varying the submitted username (e.g. "Admin@is.net" vs "admin@is.net") must not create a
    // fresh, independent fail-counter - otherwise the fail-counter/tar-pit is trivially bypassed by an
    // attacker cycling through casings, and a legitimate user's own case-inconsistent typing keeps
    // stale "suspicious" entries alive under variants that never get cleared on success.
    private static string UsernameCacheKey(string username) => UsernameKeyPrefix + username.Trim().ToUpperInvariant();

    private static string IpCacheKey(string ipAddress) => IpKeyPrefix + ipAddress.Trim();

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
