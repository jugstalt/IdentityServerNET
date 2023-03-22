using IdentityServer.Legacy.Services.EmailSender;
using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services
{
    public class EmailSenderProxy : IEmailSender
    {
        private readonly ICustomEmailSender _customEmailSender;
        public EmailSenderProxy(ICustomEmailSender customEmailSender)
        {
            _customEmailSender = customEmailSender;
        }

        #region IEmailSender

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return _customEmailSender.SendEmailAsync(email, subject, htmlMessage);
        }

        #endregion
    }
}
