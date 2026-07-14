using System;

namespace IdentityServerNET.Services.SigningCredential;

public class SigningCredentialRenewalOptions
{
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromHours(6);

    public int RenewIfOlderThanDays { get; set; } = 60;

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(15);
}
