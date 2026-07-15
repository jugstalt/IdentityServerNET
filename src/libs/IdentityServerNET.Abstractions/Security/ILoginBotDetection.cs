using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.Security;

public interface ILoginBotDetection
{
    Task<bool> IsSuspiciousUserAsync(string username);
    Task AddSuspiciousUserAsync(string username);
    Task RemoveSuspiciousUserAsync(string username);

    // For the admin UI (see Areas/Admin/Pages/BotDetection) - lists every username currently tracked
    // (not just the ones already over MaxFailCount), so an admin can see who's getting close too.
    Task<IReadOnlyCollection<LoginBotDetectionEntry>> GetSuspiciousUsersAsync();

    Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username);

    // Creates/refreshes a CAPTCHA challenge for redisplaying the login form (e.g. on a GET after the
    // user already failed enough times to be suspicious) without counting as an additional failure -
    // unlike AddSuspicousUserAndGenerateCaptchaCodeAsync, this must never increment the fail counter.
    Task<string> EnsureCaptchaCodeAsync(string username);

    Task<bool> VerifyCaptchaCodeAsync(string username, string code);

    Task BlockSuspicousUser(string username);

    // IP-based tracking is intentionally weaker than username tracking: an IP can legitimately
    // represent many unrelated users (NAT, corporate proxy, mobile carrier), so callers must only use
    // this to add friction (e.g. require a CAPTCHA) - never to call BlockSuspicousUser or otherwise
    // hard-block, which would collaterally lock out everyone sharing that IP.
    Task<bool> IsSuspiciousIpAsync(string ipAddress);
    Task AddSuspiciousIpAsync(string ipAddress);

    // Only meant for deliberate admin use (see Areas/Admin/Pages/BotDetection) - never call this from
    // the login flow itself; see IsSuspiciousIpAsync for why IP suspicion must not auto-clear.
    Task RemoveSuspiciousIpAsync(string ipAddress);

    // For the admin UI - lists every IP currently tracked (not just the ones already over
    // MaxIpFailCount).
    Task<IReadOnlyCollection<LoginBotDetectionEntry>> GetSuspiciousIpsAsync();
}
