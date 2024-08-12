using System.Threading.Tasks;

namespace IdentityServer.Nova.Abstractions.EmailSender;

public interface ICustomEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
