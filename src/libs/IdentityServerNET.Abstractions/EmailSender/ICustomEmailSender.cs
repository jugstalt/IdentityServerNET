using System.Threading.Tasks;

namespace IdentityServerNET.Abstractions.EmailSender;

public interface ICustomEmailSender
{
    Task SendEmailAsync(string email, string subject, string htmlMessage);
}
