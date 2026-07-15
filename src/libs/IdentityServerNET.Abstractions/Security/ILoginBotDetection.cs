using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.Security;

public interface ILoginBotDetection
{
    Task<bool> IsSuspiciousUserAsync(string username);
    Task AddSuspiciousUserAsync(string username);
    Task RemoveSuspiciousUserAsync(string username);

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
}
