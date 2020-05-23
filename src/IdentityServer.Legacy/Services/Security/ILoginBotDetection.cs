using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.Security
{
    public interface ILoginBotDetection
    {
        Task<bool> IsSuspiciousUserAsync(string username);
        Task AddSuspiciousUserAsync(string username);
        Task RemoveSuspiciousUserAsync(string username);

        Task<string> AddSuspicousUserAndGenerateCaptchaCodeAsync(string username);
        Task<bool> VerifyCaptchaCodeAsync(string username, string code);

        Task BlockSuspicousUser(string username);
    }
}
