using System.Threading.Tasks;

namespace IdentityServer.Legacy.Services.EmailSender
{
    public interface ICustomEmailSender
    {
        Task SendEmailAsync(string email, string subject, string htmlMessage);
    }
}
