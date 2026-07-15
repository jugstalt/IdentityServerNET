using System;

namespace IdentityServerNET.Abstractions.Security;

// Read model for the admin UI (see ILoginBotDetection.GetSuspiciousUsersAsync/GetSuspiciousIpsAsync) -
// Key is the normalized username or IP address being tracked, not a cache key.
public class LoginBotDetectionEntry
{
    public string Key { get; set; } = string.Empty;
    public int FailCount { get; set; }
    public DateTime LastFailureUtc { get; set; }
    public bool IsSuspicious { get; set; }
}
