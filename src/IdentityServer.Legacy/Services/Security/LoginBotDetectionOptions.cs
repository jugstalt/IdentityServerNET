namespace IdentityServer.Legacy.Services.Security
{
    public class LoginBotDetectionOptions
    {
        public LoginBotDetectionOptions()
        {
            MaxFailCount = 3;
            RembemberSuspiciousUserTotalMinutes = 60;
            CaptchaCodeLength = 4;
            BlockSuspiciousUserSeconds = 5;
            CaptchaCodeLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        }

        public int MaxFailCount { get; set; }
        public int RembemberSuspiciousUserTotalMinutes { get; set; }

        public int BlockSuspiciousUserSeconds { get; set; }

        public int CaptchaCodeLength { get; set; }
        public string CaptchaCodeLetters { get; set; }
    }
}
