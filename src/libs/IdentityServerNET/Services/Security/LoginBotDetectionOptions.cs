namespace IdentityServerNET.Services.Security;

public class LoginBotDetectionOptions
{
    public LoginBotDetectionOptions()
    {
        MaxFailCount = 3;
        RembemberSuspiciousUserTotalMinutes = 60;
        CaptchaCodeLength = 6;
        BlockSuspiciousUserSeconds = 5;
        CaptchaCodeLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        // Deliberately more lenient than the username thresholds above - a shared IP (corporate NAT,
        // mobile carrier, VPN) can represent many unrelated legitimate users, so it should take
        // noticeably more failures before it starts adding friction for everyone behind it.
        MaxIpFailCount = 15;
        RememberSuspiciousIpTotalMinutes = 60;
    }

    public int MaxFailCount { get; set; }
    public int RembemberSuspiciousUserTotalMinutes { get; set; }

    public int BlockSuspiciousUserSeconds { get; set; }

    public int CaptchaCodeLength { get; set; }
    public string CaptchaCodeLetters { get; set; }

    public int MaxIpFailCount { get; set; }
    public int RememberSuspiciousIpTotalMinutes { get; set; }
}
